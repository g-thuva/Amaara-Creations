using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
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
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "JWT Authentication failed: {Message}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Challenge triggered - Error: {Error}, Description: {ErrorDescription}", 
                context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.AdminAccess, policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AppPolicies.ManageCustomers, policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AppPolicies.ManageOrders, policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AppPolicies.ManageCatalog, policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AppPolicies.ManageContent, policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AppPolicies.ManageReviews, policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
    options.AddPolicy(AppPolicies.ViewReports, policy => policy.RequireRole(AppRoles.Admin, AppRoles.SuperAdmin));
});

// Register TokenService
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IRefreshSessionService, RefreshSessionService>();
builder.Services.AddScoped<IEmailService, DevelopmentEmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IMediaStorageService, LocalMediaStorageService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth-sensitive", limiter =>
    {
        limiter.PermitLimit = builder.Configuration.GetValue<int?>("Authentication:RateLimitPermitLimit") ?? 10;
        limiter.Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int?>("Authentication:RateLimitWindowMinutes") ?? 1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
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

app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<CorrelationIdMiddleware>();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads"));

// Enable static file serving for uploaded images
app.UseStaticFiles();

// Enable CORS (must be before UseAuthentication and UseAuthorization)
app.UseCors("AllowReactApp");

app.UseRateLimiter();

// Authentication & Authorization (order matters!)
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow
}));

app.MapControllers();

// Seed roles on startup. In Development, allow the app to start so /health can
// be used even before SQL Server is configured.
try
{
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { AppRoles.Admin, AppRoles.Customer, AppRoles.SuperAdmin };

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
