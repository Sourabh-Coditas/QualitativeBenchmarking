namespace KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

public record CreateSavedSearchDto
{
    public string Name { get; init; } = null!;
    public string SearchType { get; init; } = null!;
    public string FinancialYear { get; init; } = null!;

    public string? TransactionName { get; init; }
    public string? Industry { get; init; }
    public string? CompanyName { get; init; }
    public string? Purpose { get; init; }
    public string? CompanyBusinessDescription { get; init; }
    public string? ExclusionKeywords { get; init; }
    public string? AiPrompt { get; init; }
}

