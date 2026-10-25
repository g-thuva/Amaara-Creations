using Amazon;
using Amazon.S3;
using be.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Storage;

public static class StorageRegistration
{
    public static IServiceCollection AddMediaStorage(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var settings = StorageOptions.Read(configuration);
        var validator = new StorageOptionsValidator(environment, configuration);
        var validation = validator.Validate(null, settings);
        if (validation.Failed) throw new OptionsValidationException("Storage", typeof(StorageOptions), validation.Failures);
        services.AddSingleton<IValidateOptions<StorageOptions>>(validator);
        services.AddOptions<StorageOptions>().Configure(options =>
        {
            configuration.GetSection("Storage").Bind(options);
            options.Provider = configuration["Storage:Provider"] ?? configuration["Uploads:Provider"] ?? "Local";
        }).ValidateOnStart();
        services.AddScoped<LocalMediaStorageService>();
        services.AddScoped<IPublicMediaDelivery, PublicMediaDelivery>();
        services.AddScoped<IMediaCleanupQueue, MediaCleanupQueue>();
        if (settings.Provider == "S3")
        {
            services.AddSingleton<IAmazonS3>(_ =>
            {
                try
                {
                    return new AmazonS3Client(new AmazonS3Config
                    {
                        RegionEndpoint = RegionEndpoint.GetBySystemName(settings.S3.Region),
                        MaxErrorRetry = 3, RetryMode = Amazon.Runtime.RequestRetryMode.Standard,
                        Timeout = TimeSpan.FromSeconds(30),
                    });
                }
                catch (Amazon.Runtime.AmazonClientException)
                {
                    throw new be.Domain.Common.CommerceException("MEDIA_STORAGE_UNAVAILABLE", "S3 credentials are unavailable. Refresh the configured AWS profile or check the execution role.", 503);
                }
            });
            services.AddScoped<S3MediaStorageService>();
            services.AddScoped<IMediaStorageService>(provider => provider.GetRequiredService<S3MediaStorageService>());
            services.AddScoped<IAvatarStorage, S3AvatarStorage>();
            services.AddHostedService<MediaCleanupWorker>();
        }
        else
        {
            services.AddScoped<IMediaStorageService>(provider => provider.GetRequiredService<LocalMediaStorageService>());
            services.AddScoped<IAvatarStorage, LocalAvatarStorage>();
        }
        return services;
    }
}
