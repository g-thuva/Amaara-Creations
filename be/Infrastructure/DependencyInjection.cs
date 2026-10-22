using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Email;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Payments;
using be.Application.Abstractions.Storage;
using be.Application.Features.Checkout;
using be.Domain.Identity;
using be.Infrastructure.BackgroundJobs;
using be.Infrastructure.Configuration;
using be.Infrastructure.Email;
using be.Infrastructure.Identity;
using be.Infrastructure.Observability;
using be.Infrastructure.Payments.Datatrans;
using be.Infrastructure.Payments.Zahls;
using be.Infrastructure.Persistence;
using be.Infrastructure.Storage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static be.Infrastructure.Configuration.StartupSettings;

namespace be.Infrastructure;
public static class DependencyInjection
{
    public static void AddAmaaraInfrastructure(this WebApplicationBuilder builder)
    {
        ProductionSettings.Validate(builder.Configuration, builder.Environment);
        var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
        builder.Services.AddDataProtection()
            .SetApplicationName("AmaaraCreations")
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
        // Configure SQL Server with Entity Framework Core
        var connectionString = RequireConfiguration(builder, "ConnectionStrings:DefaultConnection");
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        builder.Services.AddScoped<be.Application.Abstractions.Persistence.IApplicationDbContext>(services => services.GetRequiredService<ApplicationDbContext>());
        // Configure Identity
        builder.Services.AddIdentity<User, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            // Unicode code-point length and the upper bound are checked below.
            options.Password.RequiredLength = 1;
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
        builder.Services.AddScoped<IPasswordValidator<User>, PassphraseValidator>();
        builder.Services.AddSingleton<FilePasswordBlocklist>();
        builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(30));
        // Register TokenService
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IRefreshSessionService, RefreshSessionService>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ISecondFactorService, SecondFactorService>();
        if (builder.Configuration.GetValue<bool?>("Email:Enabled") == false)
            builder.Services.AddScoped<IEmailService, DisabledEmailService>();
        else if (builder.Configuration["Email:Provider"] == "DevelopmentCapture")
        {
            if (!builder.Environment.IsDevelopment())
                throw new InvalidOperationException("Development email capture cannot run outside Development.");
            builder.Services.AddScoped<IEmailService, DevelopmentEmailService>();
        }
        else
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IMediaStorageService, LocalMediaStorageService>();
        builder.Services.AddHttpClient<IUploadScanner, HttpUploadScanner>(client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.Services.AddScoped<be.Application.Abstractions.Storage.IAvatarStorage, be.Infrastructure.Storage.LocalAvatarStorage>();
        builder.Services.Configure<CheckoutOptions>(builder.Configuration.GetSection("Checkout"));
        builder.Services.Configure<DatatransOptions>(builder.Configuration.GetSection("Datatrans"));
        builder.Services.Configure<ZahlsOptions>(builder.Configuration.GetSection("Zahls"));
        builder.Services.AddScoped<DatatransWebhookVerifier>();
        builder.Services.AddScoped<ZahlsWebhookVerifier>();
        builder.Services.AddScoped<be.Application.Abstractions.Payments.IPaymentWebhookParser, be.Infrastructure.Payments.Zahls.ZahlsWebhookParser>();
        builder.Services.AddScoped<be.Application.Abstractions.Payments.IPaymentWebhookParser, be.Infrastructure.Payments.Datatrans.DatatransWebhookParser>();
        builder.Services.AddHttpClient<DatatransPaymentGateway>(client => client.Timeout = TimeSpan.FromSeconds(Math.Clamp(builder.Configuration.GetValue<int?>("Datatrans:TimeoutSeconds") ?? 15, 5, 60)))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.Services.AddHttpClient<ZahlsPaymentGateway>(client => client.Timeout = TimeSpan.FromSeconds(Math.Clamp(builder.Configuration.GetValue<int?>("Zahls:TimeoutSeconds") ?? 15, 2, 60)))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        // The final registration is the active checkout provider. Both adapters remain
        // available for provider-qualified historical reconciliation and refunds.
        builder.Services.AddTransient<IPaymentGateway>(services => services.GetRequiredService<DatatransPaymentGateway>());
        builder.Services.AddTransient<IPaymentGateway>(services => services.GetRequiredService<ZahlsPaymentGateway>());
        builder.Services.AddHostedService<CommerceMaintenanceService>();
    }
}
