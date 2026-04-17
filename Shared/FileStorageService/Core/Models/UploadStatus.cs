namespace FileStorageService.Core.Models;

public class UploadStatus
{
    public string UploadId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public int TotalChunks { get; set; }

    public int CompletedChunksCount { get; set; }

    public List<int> PendingChunks { get; set; } = new();

    public double ProgressPercent => TotalChunks > 0
        ? Math.Round((double)CompletedChunksCount / TotalChunks * 100, 2)
        : 0;

    public bool IsComplete => CompletedChunksCount == TotalChunks;

    public UploadSessionStatus Status { get; set; }

    public long FileSizeBytes { get; set; }

    public long BytesUploaded { get; set; }
}
