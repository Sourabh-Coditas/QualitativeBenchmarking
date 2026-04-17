using System.IO.Pipelines;

namespace FileStorageService.Core.Interfaces;

public interface IStorageProvider
{
    Task<string> UploadStreamAsync(string fileName, PipeReader data, CancellationToken ct);

    Task UploadChunkAsync(string uploadId, int chunkIndex, Stream data, CancellationToken ct);

    Task<string> AssembleChunksAsync(string uploadId, int totalChunks, string fileName, long fileSizeBytes, CancellationToken ct);

    Task DeleteChunksAsync(string uploadId, CancellationToken ct);

    Task<bool> ExistsAsync(string fileName, CancellationToken ct);

    Task DeleteAsync(string fileName, CancellationToken ct);
}
