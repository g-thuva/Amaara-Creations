using be.Api.Middleware;
using be.Api.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using static be.Infrastructure.Configuration.StartupSettings;

namespace be.Api.Extensions;
public static class ApiServiceExtensions
{
    public static void AddAmaaraApi(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
        builder.Logging.AddDebug();
        builder.Services.Configure<be.Api.Configuration.ReleaseControls>(builder.Configuration.GetSection("ReleaseControls"));
        builder.Services.AddScoped<be.Application.Abstractions.Identity.IRequestActor, be.Api.Identity.HttpRequestActor>();
        builder.Services.AddScoped<be.Api.Identity.RefreshCookie>();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            string Partition(HttpContext context) => context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            options.AddPolicy("auth-sensitive", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue<int?>("Authentication:RateLimitPermitLimit") ?? 10, Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int?>("Authentication:RateLimitWindowMinutes") ?? 1), QueueLimit = 0 }));
            // Reloads restore an existing secure session; they must not consume password-attempt permits.
            options.AddPolicy("auth-refresh", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue<int?>("Authentication:RefreshRateLimitPermitLimit") ?? 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("custom-quote", context => RateLimitPartition.GetFixedWindowLimiter(Partition(context), _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("custom-upload", context => RateLimitPartition.GetFixedWindowLimiter(Partition(context), _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        // Enable CORS for React frontend
        var allowedOrigins = GetAllowedOrigins(builder);
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowReactApp", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders("Location", CorrelationIdMiddleware.HeaderName);
            });
        });
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var correlationId))
                {
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
                }
            };
        });
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            var proxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = 1;
            foreach (var proxy in proxies)
                options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
            if (proxies.Length > 0)
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
        {
            // Use camelCase for JSON property names
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Instance = context.HttpContext.Request.Path
                };
                if (context.HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var correlationId))
                {
                    problemDetails.Extensions["correlationId"] = correlationId;
                }

                return new BadRequestObjectResult(problemDetails);
            };
        });
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.OperationFilter<AnonymousSwaggerOperationFilter>();
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Amaara Creations API", Version = "v1", Description = "API for Amaara Creations E-commerce Platform" });
            // Add JWT Authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Description = "JWT Authorization header using the Bearer scheme. Enter ONLY your token (without 'Bearer' prefix). Example: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...", Name = "Authorization", In = ParameterLocation.Header, Type = SecuritySchemeType.Http, Scheme = "Bearer", BearerFormat = "JWT" });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
        });
    }
}
