using System.Collections.Concurrent;
using FileStorageService.Core.Interfaces;
using FileStorageService.Core.Models;
using Microsoft.Extensions.Options;

namespace FileStorageService.Core.Services;

public class ChunkManagerService : IChunkManager
{
    private readonly ConcurrentDictionary<string, UploadSession> _sessions = new();
    private readonly FileStorageOptions _options;

    public ChunkManagerService(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
    }

    public Task<UploadSession> CreateSessionAsync(string fileName, long fileSizeBytes, CancellationToken ct)
    {
        var chunkSizeBytes = _options.ChunkSizeBytes;
        var totalChunks = (int)Math.Ceiling((double)fileSizeBytes / chunkSizeBytes);

        var session = new UploadSession
        {
            Id = Guid.NewGuid().ToString("N"),
            FileName = fileName,
            FileSizeBytes = fileSizeBytes,
            ChunkSizeBytes = chunkSizeBytes,
            TotalChunks = totalChunks,
            CompletedChunks = new HashSet<int>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = UploadSessionStatus.Active
        };

        _sessions[session.Id] = session;

        return Task.FromResult(session);
    }

    public Task<UploadSession?> GetSessionAsync(string uploadId, CancellationToken ct)
    {
        _sessions.TryGetValue(uploadId, out var session);
        return Task.FromResult(session);
    }

    public Task MarkChunkCompleteAsync(string uploadId, int chunkIndex, CancellationToken ct)
    {
        if (!_sessions.TryGetValue(uploadId, out var session))
        {
            throw new KeyNotFoundException($"Upload session '{uploadId}' not found");
        }

        lock (session)
        {
            session.CompletedChunks.Add(chunkIndex);
            session.UpdatedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task<UploadStatus> GetStatusAsync(string uploadId, CancellationToken ct)
    {
        if (!_sessions.TryGetValue(uploadId, out var session))
        {
            throw new KeyNotFoundException($"Upload session '{uploadId}' not found");
        }

        var pendingChunks = Enumerable.Range(0, session.TotalChunks)
            .Except(session.CompletedChunks)
            .ToList();

        var bytesUploaded = (long)session.CompletedChunks.Count * session.ChunkSizeBytes;
        if (session.CompletedChunks.Contains(session.TotalChunks - 1))
        {
            var lastChunkSize = session.FileSizeBytes - ((long)(session.TotalChunks - 1) * session.ChunkSizeBytes);
            bytesUploaded = bytesUploaded - session.ChunkSizeBytes + lastChunkSize;
        }

        var status = new UploadStatus
        {
            UploadId = session.Id,
            FileName = session.FileName,
            TotalChunks = session.TotalChunks,
            CompletedChunksCount = session.CompletedChunks.Count,
            PendingChunks = pendingChunks,
            Status = session.Status,
            FileSizeBytes = session.FileSizeBytes,
            BytesUploaded = Math.Min(bytesUploaded, session.FileSizeBytes)
        };

        return Task.FromResult(status);
    }

    public Task CompleteSessionAsync(string uploadId, CancellationToken ct)
    {
        if (_sessions.TryGetValue(uploadId, out var session))
        {
            session.Status = UploadSessionStatus.Completed;
            session.UpdatedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task CancelSessionAsync(string uploadId, CancellationToken ct)
    {
        if (_sessions.TryRemove(uploadId, out var session))
        {
            session.Status = UploadSessionStatus.Cancelled;
        }

        return Task.CompletedTask;
    }
}
