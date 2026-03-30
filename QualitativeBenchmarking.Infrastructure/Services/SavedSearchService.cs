using System.IO.Compression;
using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;
using KPMG.QualitativeBenchmarking.Domain.Entities;
using KPMG.QualitativeBenchmarking.Infrastructure.Data;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Services;

public class SavedSearchService : ISavedSearchService
{
    private readonly DummyDataStore _store;
    private readonly IBenchmarkingRequestService _requestService;

    public SavedSearchService(DummyDataStore store, IBenchmarkingRequestService requestService)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _requestService = requestService ?? throw new ArgumentNullException(nameof(requestService));
    }

    public Task<IReadOnlyList<SavedSearchItemDto>> GetStandardSearchesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SavedSearchItemDto>>(BuildStandardList());
    }

    public Task<IReadOnlyList<SavedSearchItemDto>> GetCustomizedSearchesAsync(Guid? requestorUserId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SavedSearchItemDto>>(BuildCustomizedList(requestorUserId));
    }

    public Task<IReadOnlyList<SavedSearchPeriodGroupDto>> GetStandardSearchesGroupedByPeriodAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GroupByPeriod(BuildStandardList()));
    }

    public Task<IReadOnlyList<SavedSearchPeriodGroupDto>> GetCustomizedSearchesGroupedByPeriodAsync(Guid? requestorUserId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GroupByPeriod(BuildCustomizedList(requestorUserId)));
    }

    private List<SavedSearchItemDto> BuildStandardList()
    {
        return _store.GetAllRequests()
            .Where(r => r.SearchType == "Standard Search" && r.Status == "Generated")
            .OrderBy(r => r.FinancialYear).ThenBy(r => r.BenchmarkingName)
            .Select(ToSavedSearchItemDto)
            .ToList();
    }

    private List<SavedSearchItemDto> BuildCustomizedList(Guid? requestorUserId)
    {
        return _store.GetAllRequests()
            .Where(r => (r.SearchType == "Customized Search" || r.SearchType == "Customised Search") && r.Status == "Generated")
            .Where(r => !requestorUserId.HasValue || r.RequestorUserId == requestorUserId)
            .OrderBy(r => r.FinancialYear).ThenBy(r => r.BenchmarkingName)
            .Select(ToSavedSearchItemDto)
            .ToList();
    }

    private static SavedSearchItemDto ToSavedSearchItemDto(BenchmarkingRequest r)
    {
        return new SavedSearchItemDto
        {
            Id = r.Id,
            Name = r.BenchmarkingName,
            FinancialYear = r.FinancialYear,
            RequestorName = r.RequestorName,
            SearchType = r.SearchType
        };
    }

    private static IReadOnlyList<SavedSearchPeriodGroupDto> GroupByPeriod(IReadOnlyList<SavedSearchItemDto> flat)
    {
        return flat
            .GroupBy(i => i.FinancialYear, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SavedSearchPeriodGroupDto
            {
                Period = g.Key,
                Items = g.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList()
            })
            .ToList();
    }

    /// <summary>
    /// Returns a zip containing disposition output and reconciliation matrix (when available).
    /// Access: Standard Search = any user can download; Customized Search = only requestor or admin.
    /// </summary>
    public async Task<(Stream? Content, string? FileName)> DownloadSearchAsync(Guid id, Guid? requestorUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var r = _store.GetRequestById(id);
        if (r == null || !string.Equals(r.Status, "Generated", StringComparison.OrdinalIgnoreCase))
            return (null, null);

        var isStandard = string.Equals(r.SearchType, "Standard Search", StringComparison.OrdinalIgnoreCase);
        if (isStandard)
        {
            // Spec: all users can download Standard Search.
        }
        else
        {
            // Customized Search: only owner or admin.
            if (!isAdmin && r.RequestorUserId != requestorUserId)
                return (null, null);
        }

        var (mainStream, mainName) = await _requestService.GetOutputForDownloadAsync(id, "main", cancellationToken);
        var (reconStream, reconName) = await _requestService.GetOutputForDownloadAsync(id, "recon", cancellationToken);

        if (mainStream == null && reconStream == null)
            return (null, null);

        var zipStream = new MemoryStream();
        using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            if (mainStream != null)
            {
                var entry = zip.CreateEntry(mainName ?? "qualitative-benchmarking-output.xlsx");
                await using (var entryStream = entry.Open())
                await using (mainStream)
                    await mainStream.CopyToAsync(entryStream, cancellationToken);
            }
            if (reconStream != null)
            {
                var entry = zip.CreateEntry(reconName ?? "reconciliation-matrix.xlsx");
                await using (var entryStream = entry.Open())
                await using (reconStream)
                    await reconStream.CopyToAsync(entryStream, cancellationToken);
            }
        }
        zipStream.Position = 0;
        return (zipStream, "search.zip");
    }
}
