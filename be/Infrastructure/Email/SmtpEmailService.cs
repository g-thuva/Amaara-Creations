using be.Application.Abstractions.Email;
using be.Domain.Identity;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;

namespace be.Infrastructure.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailConfirmationAsync(User user, string confirmationUrl, CancellationToken cancellationToken = default)
        {
            var code = ExtractCode(confirmationUrl);
            var subject = $"{code} is your Amaara Creations verification code";
            var userName = string.IsNullOrWhiteSpace(user.Name) ? "Valued Customer" : user.Name.Trim();

            var textBody = $@"Hello {userName},

Thank you for joining Amaara Creations!

Your 6-digit verification code is:
----------------------------------------
{code}
----------------------------------------

Please enter this verification code on the screen to confirm your email address and activate your account.
This code expires in 15 minutes.

If you did not create an account, please ignore this email.

Warm regards,
The Amaara Creations Team";

            var htmlBody = BuildHtmlTemplate(
                title: "Verify Your Email",
                subtitle: $"Hello {HttpUtility.HtmlEncode(userName)}, please confirm your email address to complete your registration.",
                code: code,
                instructions: "Enter this 6-digit verification code on the screen to confirm your email and activate your account. This code expires in 15 minutes.",
                footerNote: "If you did not request this verification, you can safely ignore this message."
            );

            await SendEmailAsync(user.Email!, subject, textBody, htmlBody, cancellationToken);
        }

        public async Task SendPasswordResetAsync(User user, string resetUrl, CancellationToken cancellationToken = default)
        {
            var code = ExtractCode(resetUrl);
            var subject = $"{code} is your Amaara Creations password reset code";
            var userName = string.IsNullOrWhiteSpace(user.Name) ? "Valued Customer" : user.Name.Trim();

            var textBody = $@"Hello {userName},

We received a request to reset your password for your Amaara Creations account.

Your 6-digit password reset code is:
----------------------------------------
{code}
----------------------------------------

Please enter this code on the password reset screen along with your new password.
This code expires in 15 minutes.

If you did not request a password reset, you can safely ignore this email. Your password will remain unchanged and your account is secure.

Warm regards,
The Amaara Creations Team";

            var htmlBody = BuildHtmlTemplate(
                title: "Reset Your Password",
                subtitle: $"Hello {HttpUtility.HtmlEncode(userName)}, we received a request to reset the password for your account.",
                code: code,
                instructions: "Enter this 6-digit code on the reset password screen along with your new password. This code expires in 15 minutes.",
                footerNote: "If you did not request a password reset, you can safely ignore this message. Your account remains secure."
            );

            await SendEmailAsync(user.Email!, subject, textBody, htmlBody, cancellationToken);
        }

        public Task SendTransactionAsync(User user, string subject, string body, CancellationToken cancellationToken = default)
        {
            var config = _configuration.GetSection("Email");
            if (string.IsNullOrWhiteSpace(config["Username"]) || string.IsNullOrWhiteSpace(config["Password"]))
                throw new InvalidOperationException("Transactional email is not configured.");

            var htmlBody = BuildTransactionHtmlTemplate(subject, body, user.Name);
            return SendEmailAsync(user.Email!, subject, body, htmlBody, cancellationToken);
        }

        private static string ExtractCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var trimmed = input.Trim();
            if (Regex.IsMatch(trimmed, @"^\d{6}$"))
                return trimmed;

            try
            {
                if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
                {
                    var queryOrFragment = !string.IsNullOrEmpty(uri.Query) ? uri.Query : uri.Fragment;
                    if (queryOrFragment.Contains('?'))
                        queryOrFragment = queryOrFragment[(queryOrFragment.IndexOf('?') + 1)..];
                    else if (queryOrFragment.StartsWith('#'))
                        queryOrFragment = queryOrFragment[1..];

                    var parameters = HttpUtility.ParseQueryString(queryOrFragment);
                    var code = parameters["code"] ?? parameters["token"];
                    if (!string.IsNullOrWhiteSpace(code) && Regex.IsMatch(code.Trim(), @"^\d{6}$"))
                        return code.Trim();
                }
            }
            catch
            {
                // Fallback to regex below
            }

            var match = Regex.Match(trimmed, @"(?:code|token)=([0-9]{6})|\b([0-9]{6})\b");
            if (match.Success)
            {
                var val = !string.IsNullOrEmpty(match.Groups[1].Value) ? match.Groups[1].Value : match.Groups[2].Value;
                if (!string.IsNullOrEmpty(val)) return val;
            }

            return trimmed;
        }

        private async Task SendEmailAsync(string toEmail, string subject, string plainTextBody, string? htmlBody, CancellationToken cancellationToken)
        {
            var emailConfig = _configuration.GetSection("Email");
            var username = emailConfig["Username"];
            var password = emailConfig["Password"];
            var fromEmail = emailConfig["FromEmail"] ?? username;
            var fromName = emailConfig["FromName"] ?? "Amaara Creations";
            var host = emailConfig["Host"] ?? "smtp.gmail.com";
            var port = emailConfig.GetValue<int?>("Port") ?? 587;
            var enableSsl = emailConfig.GetValue<bool?>("EnableSsl") ?? true;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogWarning("Email delivery skipped: Username, Password or FromEmail is not configured.");
                throw new InvalidOperationException("Email delivery is not configured.");
            }

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(username, password)
                };

                var fromAddress = new MailAddress(fromEmail, fromName);
                var toAddress = new MailAddress(toEmail);

                using var mailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = !string.IsNullOrWhiteSpace(htmlBody) ? htmlBody : plainTextBody,
                    IsBodyHtml = !string.IsNullOrWhiteSpace(htmlBody)
                };

                if (!string.IsNullOrWhiteSpace(htmlBody))
                {
                    mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain"));
                }

                await client.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Email delivery completed successfully to {Recipient} with subject '{Subject}'.", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email delivery to {Recipient} failed: {ErrorMessage}", toEmail, ex.Message);
                throw new InvalidOperationException($"Email delivery failed: {ex.Message}", ex);
            }
        }

        private static string BuildHtmlTemplate(string title, string subtitle, string code, string instructions, string footerNote)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{HttpUtility.HtmlEncode(title)}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f7f6f2; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1f2937;"">
  <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f7f6f2; padding: 40px 10px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 540px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0, 0, 0, 0.06); border: 1px solid #eae7dd;"">
          <!-- Header -->
          <tr>
            <td style=""background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding: 36px 30px; text-align: center;"">
              <h1 style=""margin: 0; font-size: 24px; font-weight: 700; color: #d4af37; letter-spacing: 2px; text-transform: uppercase;"">AMAARA CREATIONS</h1>
              <p style=""margin: 6px 0 0 0; color: #94a3b8; font-size: 13px; letter-spacing: 1px;"">HANDCRAFTED ELEGANCE</p>
            </td>
          </tr>
          <!-- Body Content -->
          <tr>
            <td style=""padding: 36px 32px;"">
              <h2 style=""margin: 0 0 12px 0; font-size: 22px; font-weight: 700; color: #0f172a;"">{HttpUtility.HtmlEncode(title)}</h2>
              <p style=""margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: #475569;"">{HttpUtility.HtmlEncode(subtitle)}</p>
              
              <!-- 6-Digit OTP Box -->
              <div style=""background: #f8fafc; border: 2px dashed #cbd5e1; border-radius: 12px; padding: 24px 16px; text-align: center; margin: 28px 0;"">
                <span style=""display: block; font-size: 12px; text-transform: uppercase; font-weight: 600; color: #64748b; letter-spacing: 1.5px; margin-bottom: 8px;"">Verification Code</span>
                <span style=""font-family: 'Courier New', Courier, monospace, monospace; font-size: 38px; font-weight: 800; letter-spacing: 10px; color: #0f172a; text-indent: 10px; display: inline-block;"">{HttpUtility.HtmlEncode(code)}</span>
                <span style=""display: block; font-size: 12px; color: #94a3b8; margin-top: 8px;"">Expires in 15 minutes</span>
              </div>

              <p style=""margin: 0 0 20px 0; font-size: 14px; line-height: 1.6; color: #475569;"">{HttpUtility.HtmlEncode(instructions)}</p>
              <p style=""margin: 0; font-size: 13px; line-height: 1.5; color: #94a3b8;"">{HttpUtility.HtmlEncode(footerNote)}</p>
            </td>
          </tr>
          <!-- Footer -->
          <tr>
            <td style=""background-color: #f8fafc; padding: 20px 32px; text-align: center; border-top: 1px solid #f1f5f9;"">
              <p style=""margin: 0; font-size: 12px; color: #94a3b8;"">&copy; {DateTime.UtcNow.Year} Amaara Creations. All rights reserved.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        private static string BuildTransactionHtmlTemplate(string title, string body, string? name)
        {
            var userName = string.IsNullOrWhiteSpace(name) ? "Customer" : name.Trim();
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{HttpUtility.HtmlEncode(title)}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f7f6f2; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1f2937;"">
  <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f7f6f2; padding: 40px 10px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 540px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0, 0, 0, 0.06); border: 1px solid #eae7dd;"">
          <tr>
            <td style=""background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding: 30px; text-align: center;"">
              <h1 style=""margin: 0; font-size: 22px; font-weight: 700; color: #d4af37; letter-spacing: 2px; text-transform: uppercase;"">AMAARA CREATIONS</h1>
            </td>
          </tr>
          <tr>
            <td style=""padding: 32px;"">
              <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 700; color: #0f172a;"">{HttpUtility.HtmlEncode(title)}</h2>
              <p style=""margin: 0 0 16px 0; font-size: 15px; color: #475569;"">Hello {HttpUtility.HtmlEncode(userName)},</p>
              <div style=""font-size: 14px; line-height: 1.6; color: #334155; white-space: pre-line;"">{HttpUtility.HtmlEncode(body)}</div>
            </td>
          </tr>
          <tr>
            <td style=""background-color: #f8fafc; padding: 20px 32px; text-align: center; border-top: 1px solid #f1f5f9;"">
              <p style=""margin: 0; font-size: 12px; color: #94a3b8;"">&copy; {DateTime.UtcNow.Year} Amaara Creations. All rights reserved.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }
    }
}
