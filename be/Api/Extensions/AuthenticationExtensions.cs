using be.Application.Features.Identity;
using be.Domain.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using be.Api.Identity;
using be.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using static be.Infrastructure.Configuration.StartupSettings;

namespace be.Api.Extensions;
public static class AuthenticationExtensions
{
    public static void AddAmaaraAuthentication(this WebApplicationBuilder builder)
    {
        // Configure JWT Authentication
        var jwtKey = RequireJwtKey(builder);
        SessionPolicy.Validate(builder.Configuration);
        var jwtIssuer = RequireConfiguration(builder, "Jwt:Issuer");
        var jwtAudience = RequireConfiguration(builder, "Jwt:Audience");
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
        {
            options.SaveToken = false;
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            // Configure token validation
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = System.Security.Claims.ClaimTypes.Name,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                ValidTypes = ["JWT"],
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
            };
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    // Privileged requests use current database roles, so demotion takes effect
                    // immediately even for an unexpired access token.
                    if (context.Principal?.Identity?.IsAuthenticated == true)
                    {
                        var manager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                        var id = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        var user = id == null ? null : await manager.FindByIdAsync(id);
                        if (user == null || !user.EmailConfirmed || await manager.IsLockedOutAsync(user))
                        {
                            context.Fail("Account unavailable");
                            return;
                        }
                        if (context.Principal!.HasClaim("session_kind", "management") && !user.TwoFactorEnabled)
                        {
                            context.Fail("Management verification required");
                            return;
                        }

                        var stamp = context.Principal?.FindFirst("security_stamp")?.Value;
                        var family = context.Principal?.FindFirst("session_family")?.Value;
                        if (stamp != user.SecurityStamp || string.IsNullOrWhiteSpace(family))
                        {
                            context.Fail("Session unavailable");
                            return;
                        }
                        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                        var now = DateTime.UtcNow;
                        var session = await db.RefreshSessions.AsNoTracking().SingleOrDefaultAsync(s => s.UserId == user.Id && s.FamilyId == family && s.RevokedAt == null);
                        var roles = await manager.GetRolesAsync(user);
                        if (!AppRoles.CanSignIn(roles, context.Principal!.HasClaim("session_kind", "management")))
                        {
                            context.Fail("Session account type changed");
                            return;
                        }
                        if (session == null || !SessionPolicy.Active(session, now) || session.SecurityStampHash != SessionPolicy.Digest(user.SecurityStamp ?? "") || session.RolesHash != SessionPolicy.Roles(roles) || session.Management != context.Principal!.HasClaim("session_kind", "management"))
                        {
                            context.Fail("Session unavailable");
                            return;
                        }

                        if (session.Privileged && (!user.TwoFactorEnabled || !long.TryParse(context.Principal!.FindFirst("mfa_time")?.Value, out var verifiedAt) || session.MfaVerifiedAt?.ToUnixTimeSeconds() != verifiedAt))
                        {
                            context.Fail("Session verification changed");
                            return;
                        }
                        var identity = (System.Security.Claims.ClaimsIdentity)context.Principal!.Identity!;
                        foreach (var claim in identity.FindAll(identity.RoleClaimType).ToArray())
                            identity.RemoveClaim(claim);
                        foreach (var role in roles)
                            identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, role));
                    }
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT authentication failed: {ExceptionType}", context.Exception.GetType().Name);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT challenge: {Error}", context.Error);
                    // This challenge runs before the endpoint executes, so one retry
                    // after renewal cannot duplicate a mutation's side effects.
                    context.Response.Headers["X-Auth-Refresh"] = "required";
                    return Task.CompletedTask;
                }
            };
        });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AppPolicies.CustomerAccess, policy => policy.RequireAuthenticatedUser()
                .RequireClaim("session_kind", "storefront").RequireRole(AppRoles.Customer)
                .RequireAssertion(context => !AppRoles.AdministrativeRoles.Any(context.User.IsInRole)));
            foreach (var entry in AppPolicies.Roles)
                options.AddPolicy(entry.Key, policy => policy.RequireAuthenticatedUser().RequireRole(entry.Value)
                    .AddRequirements(new StaffAuthorization(entry.Key is AppPolicies.RefundPayments or AppPolicies.ManageStaff or AppPolicies.ManageSettings or AppPolicies.ViewReports)));
        });
        builder.Services.AddSingleton<IAuthorizationHandler, StaffAuthorizationHandler>();
    }
}
