namespace KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

public record BenchmarkingRequestListItemDto
{
    public int SrNo { get; init; }
    public Guid Id { get; init; }
    public string BenchmarkingName { get; init; } = null!;
    public string TransactionName { get; init; } = null!;
    public string FinancialYear { get; init; } = null!;
    public string SearchType { get; init; } = null!;
    public string RequestorName { get; init; } = null!;
    public DateTime CreatedAtUtc { get; init; }
    public string Status { get; init; } = null!;
}
