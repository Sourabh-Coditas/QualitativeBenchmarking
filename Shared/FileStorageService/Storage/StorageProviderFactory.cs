using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileStorageService.Storage;

public static class StorageProviderFactory
{
    public static IServiceCollection AddStorageProvider(
        this IServiceCollection services,
        string providerType)
    {
        return providerType.ToLowerInvariant() switch
        {
            "filesystem" => services.AddSingleton<IStorageProvider, FileSystemStorageProvider>(),
            "azureblob" => services.AddSingleton<IStorageProvider, AzureBlobStorageProvider>(),
            "awss3" => services.AddSingleton<IStorageProvider, AwsS3StorageProvider>(),
            _ => throw new ArgumentException($"Unknown storage provider type: {providerType}")
        };
    }

    public static IServiceCollection AddStorageProvider(
        this IServiceCollection services,
        IOptions<FileStorageOptions> options)
    {
        return AddStorageProvider(services, options.Value.StorageProvider);
    }
}
