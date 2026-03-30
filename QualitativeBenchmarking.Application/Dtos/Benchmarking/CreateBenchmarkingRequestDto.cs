namespace KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

/// <summary>
/// Request data only (user-provided business data). Does not include file paths.
/// Stored file paths are passed separately into the use case by the API layer after storage.
/// </summary>
public record CreateBenchmarkingRequestDto
{
    public string SearchType { get; init; } = null!; // "Standard Search" | "Customized Search"
    public string BenchmarkingName { get; init; } = null!;
    public string TransactionName { get; init; } = null!;
    public string Industry { get; init; } = null!;
    public string? CompanyName { get; init; }
    public string FinancialYear { get; init; } = null!;
    public string? Purpose { get; init; }
    public string CompanyBusinessDescription { get; init; } = null!;
    public string ExclusionKeywords { get; init; } = null!;
    public string AiPrompt { get; init; } = null!;
    public string RequestorName { get; init; } = null!;
    public Guid? RequestorUserId { get; init; }
}
