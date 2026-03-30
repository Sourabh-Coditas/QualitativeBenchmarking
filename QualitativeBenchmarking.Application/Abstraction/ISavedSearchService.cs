using KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

namespace KPMG.QualitativeBenchmarking.Application.Abstraction;

public interface ISavedSearchService
{
    Task<IReadOnlyList<SavedSearchItemDto>> GetStandardSearchesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedSearchItemDto>> GetCustomizedSearchesAsync(Guid? requestorUserId, CancellationToken cancellationToken = default);

    /// <summary>Standard saved searches grouped by period (<see cref="SavedSearchItemDto.FinancialYear"/>), periods ordered ascending.</summary>
    Task<IReadOnlyList<SavedSearchPeriodGroupDto>> GetStandardSearchesGroupedByPeriodAsync(CancellationToken cancellationToken = default);

    /// <summary>Customized saved searches grouped by period, periods ordered ascending.</summary>
    Task<IReadOnlyList<SavedSearchPeriodGroupDto>> GetCustomizedSearchesGroupedByPeriodAsync(Guid? requestorUserId, CancellationToken cancellationToken = default);

    Task<(Stream? Content, string? FileName)> DownloadSearchAsync(Guid id, Guid? requestorUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
