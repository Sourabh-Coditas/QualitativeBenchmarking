using FileStorageService.Core.Models;

namespace FileStorageService.Core.Interfaces;

public interface IChunkManager
{
    Task<UploadSession> CreateSessionAsync(string fileName, long fileSizeBytes, CancellationToken ct);

    Task<UploadSession?> GetSessionAsync(string uploadId, CancellationToken ct);

    Task MarkChunkCompleteAsync(string uploadId, int chunkIndex, CancellationToken ct);

    Task<UploadStatus> GetStatusAsync(string uploadId, CancellationToken ct);

    Task CompleteSessionAsync(string uploadId, CancellationToken ct);

    Task CancelSessionAsync(string uploadId, CancellationToken ct);
}
