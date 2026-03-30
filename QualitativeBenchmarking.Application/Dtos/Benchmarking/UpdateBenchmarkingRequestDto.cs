namespace KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

public record UpdateBenchmarkingRequestDto
{
    public string? SearchType { get; init; }
    public string? BenchmarkingName { get; init; }
    public string? TransactionName { get; init; }
    public string? Industry { get; init; }
    public string? CompanyName { get; init; }
    public string? FinancialYear { get; init; }
    public string? Purpose { get; init; }
    public string? CompanyBusinessDescription { get; init; }
    public string? ExclusionKeywords { get; init; }
    public string? AiPrompt { get; init; }
}
