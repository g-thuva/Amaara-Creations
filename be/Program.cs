using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using be.Data;
using be.Infrastructure;
using be.Models;
using be.Security;
using be.Services;
using be.Services.Checkout;
using be.Services.Payments;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
builder.Logging.AddDebug();

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
builder.Services.AddDataProtection()
    .SetApplicationName("AmaaraCreations")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

// Configure SQL Server with Entity Framework Core
var connectionString = RequireConfiguration(builder, "ConnectionStrings:DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = builder.Configuration.GetValue<bool?>("Authentication:RequireConfirmedEmail") ?? true;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(builder.Configuration.GetValue<int?>("Authentication:LockoutMinutes") ?? 10);
    options.Lockout.MaxFailedAccessAttempts = builder.Configuration.GetValue<int?>("Authentication:MaxFailedAccessAttempts") ?? 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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
                if (user == null || await manager.IsLockedOutAsync(user)) { context.Fail("Account unavailable"); return; }
                var identity = (System.Security.Claims.ClaimsIdentity)context.Principal!.Identity!;
                foreach (var claim in identity.FindAll(identity.RoleClaimType).ToArray()) identity.RemoveClaim(claim);
                foreach (var role in await manager.GetRolesAsync(user)) identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, role));
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

// Register TokenService
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRefreshSessionService, RefreshSessionService>();
if (builder.Configuration["Email:Provider"] == "DevelopmentCapture")
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Development email capture cannot run outside Development.");
    builder.Services.AddScoped<IEmailService, DevelopmentEmailService>();
}
else builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<InventoryOperations>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IMediaStorageService, LocalMediaStorageService>();
builder.Services.AddScoped<ICustomStickerPricingService, CustomStickerPricingService>();
builder.Services.Configure<CheckoutOptions>(builder.Configuration.GetSection("Checkout"));
builder.Services.Configure<DatatransOptions>(builder.Configuration.GetSection("Datatrans"));
builder.Services.Configure<ZahlsOptions>(builder.Configuration.GetSection("Zahls"));
builder.Services.AddScoped<CheckoutPricingService>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddScoped<InventoryReservationService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<OrderLifecycleService>();
builder.Services.AddScoped<PaymentReconciliationService>();
builder.Services.AddScoped<RefundService>();
builder.Services.AddScoped<DatatransWebhookVerifier>();
builder.Services.AddScoped<ZahlsWebhookVerifier>();
builder.Services.AddScoped<IPaymentGatewayRouter, PaymentGatewayRouter>();
builder.Services.AddHttpClient<DatatransPaymentGateway>(client =>
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(builder.Configuration.GetValue<int?>("Datatrans:TimeoutSeconds") ?? 15, 5, 60)))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient<ZahlsPaymentGateway>(client =>
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(builder.Configuration.GetValue<int?>("Zahls:TimeoutSeconds") ?? 15, 2, 60)))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
// The final registration is the active checkout provider. Both adapters remain
// available for provider-qualified historical reconciliation and refunds.
builder.Services.AddTransient<IPaymentGateway>(services => services.GetRequiredService<DatatransPaymentGateway>());
builder.Services.AddTransient<IPaymentGateway>(services => services.GetRequiredService<ZahlsPaymentGateway>());
builder.Services.AddHostedService<CommerceMaintenanceService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    string Partition(HttpContext context) => context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    options.AddPolicy("auth-sensitive", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = builder.Configuration.GetValue<int?>("Authentication:RateLimitPermitLimit") ?? 10,
        Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int?>("Authentication:RateLimitWindowMinutes") ?? 1), QueueLimit = 0
    }));
    options.AddPolicy("custom-quote", context => RateLimitPartition.GetFixedWindowLimiter(Partition(context), _ => new FixedWindowRateLimiterOptions
    { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("custom-upload", context => RateLimitPartition.GetFixedWindowLimiter(Partition(context), _ => new FixedWindowRateLimiterOptions
    { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
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
    options.KnownNetworks.Clear(); options.KnownProxies.Clear(); options.ForwardLimit = 1;
    foreach (var proxy in proxies) options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
    if (proxies.Length > 0) options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
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
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Amaara Creations API",
        Version = "v1",
        Description = "API for Amaara Creations E-commerce Platform"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter ONLY your token (without 'Bearer' prefix). Example: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ReleaseTelemetryMiddleware>();
app.UseExceptionHandler();
app.UseMiddleware<CommerceExceptionMiddleware>();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads"));
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data", "private-media"));

// Enable static file serving for uploaded images
app.UseStaticFiles();

// Enable CORS (must be before UseAuthentication and UseAuthorization)
app.UseCors("AllowReactApp");

// Authentication & Authorization (order matters!)
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow
}));

app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (ApplicationDbContext db, CancellationToken ct) =>
{
    try
    {
        if (!await db.Database.CanConnectAsync(ct)) return Results.Json(new { status = "not_ready" }, statusCode: 503);
        if (db.Database.IsRelational() && (await db.Database.GetPendingMigrationsAsync(ct)).Any())
            return Results.Json(new { status = "not_ready" }, statusCode: 503);
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception) when (!ct.IsCancellationRequested) { return Results.Json(new { status = "not_ready" }, statusCode: 503); }
});

// Seed roles on startup. In Development, allow the app to start so /health can
// be used even before SQL Server is configured.
try
{
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = AppRoles.AdministrativeRoles.Append(AppRoles.Customer);

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminBootstrapEnabled = app.Configuration.GetValue<bool>("AdminBootstrap:Enabled");
        if (adminBootstrapEnabled)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            var adminEmail = RequireRuntimeConfiguration(app, "AdminBootstrap:Email");
            var adminPassword = RequireRuntimeConfiguration(app, "AdminBootstrap:Password");
            if (adminPassword.Length < 12)
            {
                throw new InvalidOperationException("AdminBootstrap:Password must be at least 12 characters.");
            }

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                logger.LogInformation("Creating configured bootstrap admin user.");

                var adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "Admin User",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Configured bootstrap admin user could not be created: {string.Join(", ", result.Errors.Select(e => e.Code))}");
                }

                var roleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.SuperAdmin);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Configured bootstrap admin role could not be assigned: {string.Join(", ", roleResult.Errors.Select(e => e.Code))}");
                }

                logger.LogInformation("Configured bootstrap admin user created.");
            }
            else
            {
                var isInAdminRole = await userManager.IsInRoleAsync(existingAdmin, AppRoles.Admin);
                var isInSuperAdminRole = await userManager.IsInRoleAsync(existingAdmin, AppRoles.SuperAdmin);
                if (!isInAdminRole && !isInSuperAdminRole)
                {
                    var roleResult = await userManager.AddToRoleAsync(existingAdmin, AppRoles.SuperAdmin);
                    if (!roleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Configured bootstrap admin role could not be assigned: {string.Join(", ", roleResult.Errors.Select(e => e.Code))}");
                    }

                    logger.LogInformation("Admin role added to configured bootstrap user.");
                }
            }
        }

        if (app.Environment.IsDevelopment())
        {
            await CustomBuilderDevelopmentSeeder.SeedAsync(scope.ServiceProvider);
        }
    }
}
catch (Exception ex) when (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Database role/admin bootstrap skipped. Configure SQL Server and run migrations for API endpoints that require data.");
}

app.Run();

static string RequireConfiguration(WebApplicationBuilder builder, string key)
{
    var value = builder.Configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"{key} is required. Configure it through appsettings.Development.json, user secrets, or environment variables.");
    }

    return value;
}

static string RequireRuntimeConfiguration(WebApplication app, string key)
{
    var value = app.Configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"{key} is required when AdminBootstrap:Enabled is true.");
    }

    return value;
}

static string RequireJwtKey(WebApplicationBuilder builder)
{
    var jwtKey = RequireConfiguration(builder, "Jwt:Key");
    if (jwtKey.Length < 32)
    {
        throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
    }

    var knownDevelopmentKey = string.Equals(
        jwtKey,
        "DevelopmentOnlyJwtSigningKey-Change-With-User-Secrets",
        StringComparison.Ordinal);

    if (!builder.Environment.IsDevelopment() && knownDevelopmentKey)
    {
        throw new InvalidOperationException("Production cannot start with the development JWT signing key.");
    }

    return jwtKey;
}

static string[] GetAllowedOrigins(WebApplicationBuilder builder)
{
    var origins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()?
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .ToArray() ?? Array.Empty<string>();

    if (origins.Length == 0)
    {
        if (builder.Environment.IsDevelopment())
        {
            return new[] { "http://localhost:5173", "http://127.0.0.1:5173" };
        }

        throw new InvalidOperationException("Cors:AllowedOrigins must be configured outside Development.");
    }

    return origins;
}

public partial class Program { }
