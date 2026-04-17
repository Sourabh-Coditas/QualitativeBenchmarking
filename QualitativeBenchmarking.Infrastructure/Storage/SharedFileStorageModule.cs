using System.IO.Pipelines;
using FileStorageService.Core.Interfaces;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Storage;

/// <summary>
/// Application-side adapter to the copied <c>Shared/FileStorageService</c> library.
/// Host projects must call <c>AddFileStorageServices</c> from that library at startup.
/// QB code must not reference Azure/AWS SDKs for storage—only <see cref="IStorageProvider"/> here.
/// </summary>
public sealed class SharedFileStorageModule
{
    public SharedFileStorageModule(
        IStorageProvider storageProvider,
        IChunkManager chunkManager,
        IAssemblyThrottler assemblyThrottler)
    {
        Storage = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        Chunks = chunkManager ?? throw new ArgumentNullException(nameof(chunkManager));
        Assembly = assemblyThrottler ?? throw new ArgumentNullException(nameof(assemblyThrottler));
    }

    public IStorageProvider Storage { get; }

    public IChunkManager Chunks { get; }

    public IAssemblyThrottler Assembly { get; }

    public Task<string> UploadStreamAsync(string fileName, PipeReader reader, CancellationToken cancellationToken)
        => Storage.UploadStreamAsync(fileName, reader, cancellationToken);

    public Task<bool> ExistsAsync(string pathOrName, CancellationToken cancellationToken)
        => Storage.ExistsAsync(pathOrName, cancellationToken);

    public Task DeleteAsync(string pathOrName, CancellationToken cancellationToken)
        => Storage.DeleteAsync(pathOrName, cancellationToken);
}
