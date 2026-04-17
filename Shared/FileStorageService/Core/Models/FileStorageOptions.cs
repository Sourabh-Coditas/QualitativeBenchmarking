namespace FileStorageService.Core.Models;

/// <summary>
/// Bound from configuration section <c>FileStorage</c>.
/// </summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string StorageProvider { get; set; } = "FileSystem";

    public int ChunkSizeInMB { get; set; } = 10;

    public int MaxFileSizeInGB { get; set; } = 2;

    public string TempDirectory { get; set; } = "./temp-uploads";

    public string UploadDirectory { get; set; } = "./uploads";

    public List<string> AllowedExtensions { get; set; } = new();

    public int SessionExpirationHours { get; set; } = 24;

    public int MaxConcurrentAssemblies { get; set; } = 5;

    public int AssemblyTimeoutMinutes { get; set; } = 10;

    public int ChunkSizeBytes => ChunkSizeInMB * 1024 * 1024;

    public long MaxFileSizeBytes => (long)MaxFileSizeInGB * 1024 * 1024 * 1024;
}

public class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "uploads";
}

public class AwsS3Options
{
    public const string SectionName = "AwsS3";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string BucketName { get; set; } = "uploads";

    public string Region { get; set; } = "us-east-1";

    public string? ServiceUrl { get; set; }
}
