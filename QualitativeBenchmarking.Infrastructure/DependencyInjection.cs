using FileStorageService;
using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Infrastructure.Configuration;
using KPMG.QualitativeBenchmarking.Infrastructure.Data;
using KPMG.QualitativeBenchmarking.Infrastructure.Services;
using KPMG.QualitativeBenchmarking.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace KPMG.QualitativeBenchmarking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFileStorageServices(configuration);

        services.Configure<FileUploadSettings>(options =>
            configuration.GetSection(FileUploadSettings.SectionName).Bind(options));
        services.Configure<DummyDataFileSettings>(options =>
            configuration.GetSection(DummyDataFileSettings.SectionName).Bind(options));
        services.Configure<AiServiceSettings>(options =>
            configuration.GetSection(AiServiceSettings.SectionName).Bind(options));

        services.AddHttpClient();

        services.AddHttpClient<IAiBenchmarkingService, AiBenchmarkingService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<AiServiceSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/'));
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        services.AddSingleton<DummyDataStore>();
        services.AddSingleton<SharedFileStorageModule>();
        services.AddScoped<IFileStorageService, QualitativeFileStorageService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IPromptTemplateService, PromptTemplateService>();
        services.AddScoped<IBenchmarkingRequestService, BenchmarkingRequestService>();
        services.AddScoped<ISavedSearchService, SavedSearchService>();
        return services;
    }

    /// <summary>
    /// Ensures dummy data is loaded from file (or seeded and saved). Call from Program.cs after Build().
    /// </summary>
    public static void UseDummyData(this IServiceProvider provider)
    {
        provider.GetRequiredService<DummyDataStore>().EnsureLoaded();
    }
}
