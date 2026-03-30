namespace KPMG.QualitativeBenchmarking.Application.Abstraction;

public interface IFileStorageService
{
    Task<string> StoreAsync(Stream content, string fileName, string? subPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a file under the benchmarking uploads root (e.g. User's Downloads\Uploads) in the given request folder.
    /// Used for Current Year, Previous Year, and Column Mapping Excel files. Same requestFolderName groups files for one request.
    /// </summary>
    Task<string> StoreBenchmarkingFileAsync(Stream content, string fileName, string requestFolderName, CancellationToken cancellationToken = default);

    Task<Stream?> GetAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string path, CancellationToken cancellationToken = default);
}
