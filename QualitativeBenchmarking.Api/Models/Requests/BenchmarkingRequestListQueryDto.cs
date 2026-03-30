namespace KPMG.QualitativeBenchmarking.Api.Models.Requests;

/// <summary>Query parameters for GET /api/benchmarking-requests.</summary>
public sealed class BenchmarkingRequestListQueryDto
{
    public string? BenchmarkingName { get; set; }
    public string? TransactionName { get; set; }
    public string? FinancialYear { get; set; }
    public string? SearchType { get; set; }
    public string? RequestorName { get; set; }
    public string? Status { get; set; }
    public bool MyRequestsOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
