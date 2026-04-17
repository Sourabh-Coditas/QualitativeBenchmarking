namespace KPMG.QualitativeBenchmarking.Application.Abstraction;

public interface IFileStorageService
{
    /// <summary>Returns a storage reference: local path (filesystem backend) or HTTPS URL (blob backend).</summary>
    Task<string> StoreAsync(Stream content, string fileName, string? subPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores benchmarking Excel inputs; same <paramref name="requestFolderName"/> groups files for one request.
    /// Returns a storage reference (path or blob URL) from the configured storage provider.
    /// </summary>
    Task<string> StoreBenchmarkingFileAsync(Stream content, string fileName, string requestFolderName, CancellationToken cancellationToken = default);

    /// <summary>Opens a stream for a previously stored reference (local path or HTTPS blob URL).</summary>
    Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);
}
