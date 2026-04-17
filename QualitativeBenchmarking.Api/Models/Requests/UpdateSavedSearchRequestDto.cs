namespace KPMG.QualitativeBenchmarking.Api.Models.Requests;

public sealed class UpdateSavedSearchRequestDto
{
    public string? Name { get; init; }
    public string? FinancialYear { get; init; }
    public string? TransactionName { get; init; }
    public string? Industry { get; init; }
    public string? CompanyName { get; init; }
    public string? Purpose { get; init; }
    public string? CompanyBusinessDescription { get; init; }
    public string? ExclusionKeywords { get; init; }
    public string? AiPrompt { get; init; }
}

