namespace KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

/// <summary>
/// Saved searches for one period (e.g. financial year). Use for dropdown UI: one group per period.
/// </summary>
public record SavedSearchPeriodGroupDto
{
    /// <summary>Period label, same as <see cref="SavedSearchItemDto.FinancialYear"/> (e.g. FY 2023-24).</summary>
    public string Period { get; init; } = null!;

    public IReadOnlyList<SavedSearchItemDto> Items { get; init; } = Array.Empty<SavedSearchItemDto>();
}
