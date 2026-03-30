namespace KPMG.QualitativeBenchmarking.Infrastructure.Configuration;

public class FileUploadSettings
{
    public const string SectionName = "FileUploadSettings";

    public string BasePath { get; set; } = "uploads";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
    public string[] AllowedExtensions { get; set; } = { ".xls", ".xlsx" };

    /// <summary>
    /// Root folder for benchmarking request uploads (Current Year, Previous Year, Column Mapping files).
    /// When null or empty, defaults to User's Downloads\Uploads folder on the current machine.
    /// </summary>
    public string? BenchmarkingUploadsRoot { get; set; }
}
