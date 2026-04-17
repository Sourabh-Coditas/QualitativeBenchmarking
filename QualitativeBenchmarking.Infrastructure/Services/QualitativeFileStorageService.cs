using System.IO.Pipelines;
using System.Net.Http;
using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Infrastructure.Configuration;
using KPMG.QualitativeBenchmarking.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Services;

/// <summary>
/// Qualitative Benchmarking file storage: all persistence goes through <see cref="SharedFileStorageModule"/>
/// (shared <c>FileStorageService</c> assembly). HTTP(S) reads for blob URLs use <see cref="IHttpClientFactory"/> only—no storage SDKs here.
/// </summary>
public sealed class QualitativeFileStorageService : IFileStorageService
{
    private readonly SharedFileStorageModule _shared;
    private readonly FileUploadSettings _uploadSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QualitativeFileStorageService> _logger;

    public QualitativeFileStorageService(
        SharedFileStorageModule shared,
        IOptions<FileUploadSettings> uploadSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<QualitativeFileStorageService> logger)
    {
        _shared = shared ?? throw new ArgumentNullException(nameof(shared));
        _uploadSettings = uploadSettings?.Value ?? throw new ArgumentNullException(nameof(uploadSettings));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> StoreAsync(Stream content, string fileName, string? subPath = null, CancellationToken cancellationToken = default)
    {
        ThrowIfExtensionNotAllowed(fileName);

        var uploadFileName = string.IsNullOrWhiteSpace(subPath)
            ? fileName
            : $"{SanitizeSegment(subPath)}__{Path.GetFileName(fileName)}";

        var reader = PipeReader.Create(content);
        try
        {
            return await _shared.UploadStreamAsync(uploadFileName, reader, cancellationToken);
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }

    public Task<string> StoreBenchmarkingFileAsync(
        Stream content,
        string fileName,
        string requestFolderName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfExtensionNotAllowed(fileName);
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(safeName))
            safeName = Guid.NewGuid().ToString("N") + ".xlsx";

        var uploadFileName = $"{SanitizeSegment(requestFolderName)}__{Guid.NewGuid():N}__{safeName}";
        return StoreWithReaderAsync(content, uploadFileName, cancellationToken);
    }

    private async Task<string> StoreWithReaderAsync(Stream content, string uploadFileName, CancellationToken cancellationToken)
    {
        var reader = PipeReader.Create(content);
        try
        {
            return await _shared.UploadStreamAsync(uploadFileName, reader, cancellationToken);
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }

    public async Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (IsHttpUrl(path))
            return await OpenBlobHttpStreamAsync(path, cancellationToken);

        if (!await _shared.ExistsAsync(path, cancellationToken))
            return null;

        return File.OpenRead(path);
    }

    public async Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            return true;

        try
        {
            var key = IsHttpUrl(path) ? GetBlobStorageKeyFromPublicUri(path) : path;
            await _shared.DeleteAsync(key, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Delete failed for {Path}", path);
            return false;
        }
    }

    private async Task<Stream?> OpenBlobHttpStreamAsync(string blobUri, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(15);
            return await client.GetStreamAsync(new Uri(blobUri), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open blob stream for {Uri}", blobUri);
            return null;
        }
    }

    /// <summary>
    /// Shared delete API expects blob name (path inside container) for Azure, not the public URL.
    /// </summary>
    private static string GetBlobStorageKeyFromPublicUri(string blobUri)
    {
        var uri = new Uri(blobUri);
        var path = uri.AbsolutePath.TrimStart('/');
        var slash = path.IndexOf('/');
        if (slash < 0)
            return path;
        return path[(slash + 1)..];
    }

    private static bool IsHttpUrl(string value)
        => value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
           || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static string SanitizeSegment(string segment)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var s = string.Join("_", segment.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrEmpty(s) ? "upload" : s;
    }

    private void ThrowIfExtensionNotAllowed(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (_uploadSettings.AllowedExtensions is not { Length: > 0 })
            return;

        var allowed = _uploadSettings.AllowedExtensions.Any(e =>
            string.Equals(ext, e.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!allowed)
            throw new InvalidOperationException($"File extension '{ext}' is not allowed.");
    }
}
