namespace FileStorageService.Core.Models;

public class UploadSession
{
    public string Id { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public int ChunkSizeBytes { get; set; }

    public int TotalChunks { get; set; }

    public HashSet<int> CompletedChunks { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UploadSessionStatus Status { get; set; } = UploadSessionStatus.Active;
}

public enum UploadSessionStatus
{
    Active,
    Assembling,
    Completed,
    Cancelled,
    Failed
}
