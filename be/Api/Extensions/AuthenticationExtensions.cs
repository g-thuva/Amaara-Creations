using be.Application.Features.Identity;
using be.Domain.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using static be.Infrastructure.Configuration.StartupSettings;

namespace be.Api.Extensions;
public static class AuthenticationExtensions
{
    public static void AddAmaaraAuthentication(this WebApplicationBuilder builder)
    {
        // Configure JWT Authentication
        var jwtKey = RequireJwtKey(builder);
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
                ClockSkew = TimeSpan.FromSeconds(30)
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
                        if (user == null || await manager.IsLockedOutAsync(user))
                        {
                            context.Fail("Account unavailable");
                            return;
                        }

                        var identity = (System.Security.Claims.ClaimsIdentity)context.Principal!.Identity!;
                        foreach (var claim in identity.FindAll(identity.RoleClaimType).ToArray())
                            identity.RemoveClaim(claim);
                        foreach (var role in await manager.GetRolesAsync(user))
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
                    return Task.CompletedTask;
                }
            };
        });
        builder.Services.AddAuthorization(options =>
        {
            foreach (var entry in AppPolicies.Roles)
                options.AddPolicy(entry.Key, policy => policy.RequireRole(entry.Value));
        });
    }
}
