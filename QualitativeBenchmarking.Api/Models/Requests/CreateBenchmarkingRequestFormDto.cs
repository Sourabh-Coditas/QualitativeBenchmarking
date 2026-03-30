namespace KPMG.QualitativeBenchmarking.Api.Models.Requests;

/// <summary>
/// Form fields for POST /api/benchmarking-requests (multipart form data).
/// Current Year and Column Mapping files are required; Previous Year file is optional.
/// Files are submitted as separate IFormFile parameters; the controller maps this to CreateBenchmarkingRequestDto.
/// </summary>
public sealed class CreateBenchmarkingRequestFormDto
{
    public string? SearchType { get; set; }
    public string? BenchmarkingName { get; set; }
    public string? TransactionName { get; set; }
    public string? Industry { get; set; }
    public string? CompanyName { get; set; }
    public string? FinancialYear { get; set; }
    public string? Purpose { get; set; }
    public string? CompanyBusinessDescription { get; set; }
    public string? ExclusionKeywords { get; set; }
    public string? AiPrompt { get; set; }
    public string? RequestorName { get; set; }
    public Guid? RequestorUserId { get; set; }
}
