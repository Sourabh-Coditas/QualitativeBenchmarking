namespace KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

public record BenchmarkingRequestListFilterDto
{
    public string? BenchmarkingName { get; init; }
    public string? TransactionName { get; init; }
    public string? FinancialYear { get; init; }
    public string? SearchType { get; init; }
    public string? RequestorName { get; init; }
    public string? Status { get; init; }
    public bool MyRequestsOnly { get; init; }
    public Guid? RequestorUserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
