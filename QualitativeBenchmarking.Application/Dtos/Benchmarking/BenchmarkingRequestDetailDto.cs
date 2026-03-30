namespace KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

public record BenchmarkingRequestDetailDto
{
    public Guid Id { get; init; }
    public string SearchType { get; init; } = null!;
    public string BenchmarkingName { get; init; } = null!;
    public string TransactionName { get; init; } = null!;
    public string Industry { get; init; } = null!;
    public string? CompanyName { get; init; }
    public string FinancialYear { get; init; } = null!;
    public string? Purpose { get; init; }
    public string CompanyBusinessDescription { get; init; } = null!;
    public string ExclusionKeywords { get; init; } = null!;
    public string AiPrompt { get; init; } = null!;
    public string? CurrentYearFileName { get; init; }
    public string? PreviousYearFileName { get; init; }
    public string? ColumnMappingFileName { get; init; }
    public string Status { get; init; } = null!;
    public string? DownloadMain { get; init; }
    public string? DownloadRecon { get; init; }
    public string? ProcessingError { get; init; }
    public string RequestorName { get; init; } = null!;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}
