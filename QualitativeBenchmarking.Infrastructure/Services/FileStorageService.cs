using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly FileUploadSettings _settings;

    public FileStorageService(IOptions<FileUploadSettings> settings)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<string> StoreAsync(Stream content, string fileName, string? subPath = null, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(_settings.BasePath, subPath ?? string.Empty);
        Directory.CreateDirectory(dir);
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(safeName))
            safeName = Guid.NewGuid().ToString("N");
        var path = Path.Combine(dir, $"{Guid.NewGuid():N}_{safeName}");
        await using var fs = File.Create(path);
        await content.CopyToAsync(fs, cancellationToken);
        return path;
    }

    public async Task<string> StoreBenchmarkingFileAsync(Stream content, string fileName, string requestFolderName, CancellationToken cancellationToken = default)
    {
        var baseDir = !string.IsNullOrWhiteSpace(_settings.BenchmarkingUploadsRoot)
            ? _settings.BenchmarkingUploadsRoot
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Uploads");
        var dir = Path.Combine(baseDir, requestFolderName);
        Directory.CreateDirectory(dir);
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(safeName))
            safeName = Guid.NewGuid().ToString("N") + ".xlsx";
        var path = Path.Combine(dir, $"{Guid.NewGuid():N}_{safeName}");
        await using var fs = File.Create(path);
        await content.CopyToAsync(fs, cancellationToken);
        return path;
    }

    public Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(path));
    }

    public Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
