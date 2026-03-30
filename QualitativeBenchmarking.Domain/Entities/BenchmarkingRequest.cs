namespace KPMG.QualitativeBenchmarking.Domain.Entities;

public class BenchmarkingRequest
{
    public Guid Id { get; set; }
    public string SearchType { get; set; } = null!; // "Standard Search" | "Customized Search"
    public string BenchmarkingName { get; set; } = null!;
    public string TransactionName { get; set; } = null!;
    public string Industry { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string FinancialYear { get; set; } = null!; // e.g. "FY 2016-17"
    public string? Purpose { get; set; }
    public string CompanyBusinessDescription { get; set; } = null!;
    public string ExclusionKeywords { get; set; } = null!;
    public string AiPrompt { get; set; } = null!;
    public string? CurrentYearFilePath { get; set; }
    public string? PreviousYearFilePath { get; set; }
    public string? ColumnMappingFilePath { get; set; }
    public string? OutputFilePath { get; set; }
    public string? ReconOutputFilePath { get; set; }

    // AI service output references (when output is not stored locally)
    public string? AiMainReportPath { get; set; }
    public string? AiReconReportPath { get; set; }
    public string? AiDownloadMain { get; set; }
    public string? AiDownloadRecon { get; set; }
    public string? ProcessingError { get; set; }
    public string Status { get; set; } = "Submitted"; // Submitted | InProcess | Generated
    public string RequestorName { get; set; } = null!;
    public Guid? RequestorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
