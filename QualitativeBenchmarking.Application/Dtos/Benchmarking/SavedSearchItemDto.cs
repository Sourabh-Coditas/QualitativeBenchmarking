namespace KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

public record SavedSearchItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!; // BenchmarkingName
    public string FinancialYear { get; init; } = null!;
    public string RequestorName { get; init; } = null!;
    public string SearchType { get; init; } = null!; // "Standard Search" | "Customized Search"
}
