using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using FileStorageService.Core.Services;
using FileStorageService.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileStorageService;

/// <summary>
/// Registers file storage services for use inside a modular monolith or any host that uses
/// Microsoft.Extensions.DependencyInjection. This assembly does not register HTTP endpoints;
/// expose your own controllers or minimal APIs and inject <see cref="IStorageProvider"/>,
/// <see cref="IChunkManager"/>, and <see cref="IAssemblyThrottler"/> from your modules.
/// </summary>
public static class FileStorageServiceExtensions
{
    /// <summary>
    /// Registers options, chunk manager, assembly throttling, and the configured <see cref="IStorageProvider"/>.
    /// </summary>
    public static IServiceCollection AddFileStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(
            configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<AzureBlobOptions>(
            configuration.GetSection(AzureBlobOptions.SectionName));
        services.Configure<AwsS3Options>(
            configuration.GetSection(AwsS3Options.SectionName));

        services.AddSingleton<IChunkManager, ChunkManagerService>();
        services.AddSingleton<IAssemblyThrottler, AssemblyThrottler>();

        var opts = configuration
            .GetSection(FileStorageOptions.SectionName)
            .Get<FileStorageOptions>() ?? new FileStorageOptions();

        services.AddStorageProvider(opts.StorageProvider);

        return services;
    }
}
