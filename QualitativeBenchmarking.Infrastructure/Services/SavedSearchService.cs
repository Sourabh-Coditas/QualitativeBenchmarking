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

    public Task<SavedSearchDetailDto?> GetByIdAsync(Guid id, Guid? currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var s = _store.GetSearchById(id);
        if (s == null) return Task.FromResult<SavedSearchDetailDto?>(null);
        if (!CanView(s, currentUserId, isAdmin)) return Task.FromResult<SavedSearchDetailDto?>(null);
        return Task.FromResult<SavedSearchDetailDto?>(ToSavedSearchDetailDto(s));
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
        return _store.GetAllSearches()
            .Where(s => IsStandardSearch(s.SearchType))
            .OrderBy(s => s.FinancialYear).ThenBy(s => s.Name)
            .Select(ToSavedSearchItemDto)
            .ToList();
    }

    private List<SavedSearchItemDto> BuildCustomizedList(Guid? requestorUserId)
    {
        return _store.GetAllSearches()
            .Where(s => IsCustomizedSearch(s.SearchType))
            .Where(s => !requestorUserId.HasValue || s.RequestorUserId == requestorUserId)
            .OrderBy(s => s.FinancialYear).ThenBy(s => s.Name)
            .Select(ToSavedSearchItemDto)
            .ToList();
    }

    private static SavedSearchItemDto ToSavedSearchItemDto(SavedSearch s)
    {
        return new SavedSearchItemDto
        {
            Id = s.Id,
            Name = s.Name,
            FinancialYear = s.FinancialYear,
            RequestorName = s.RequestorName,
            SearchType = s.SearchType
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

    public Task<SavedSearchDetailDto> CreateAsync(CreateSavedSearchDto dto, Guid? currentUserId, string currentUsername, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var normalizedSearchType = NormalizeSearchType(dto.SearchType);
        if (normalizedSearchType == null)
            throw new ArgumentException("SearchType must be either 'Standard Search' or 'Customized Search'.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required.", nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.FinancialYear))
            throw new ArgumentException("FinancialYear is required.", nameof(dto));

        if (IsStandardSearch(normalizedSearchType) && !isAdmin)
            throw new UnauthorizedAccessException("Only admins can create standard searches.");
        if (IsCustomizedSearch(normalizedSearchType) && !currentUserId.HasValue)
            throw new UnauthorizedAccessException("Authenticated user is required to create customized searches.");

        var search = new SavedSearch
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            SearchType = normalizedSearchType,
            FinancialYear = dto.FinancialYear.Trim(),
            RequestorUserId = IsCustomizedSearch(normalizedSearchType) ? currentUserId : currentUserId,
            RequestorName = string.IsNullOrWhiteSpace(currentUsername) ? "User" : currentUsername.Trim(),
            IsAdminManaged = IsStandardSearch(normalizedSearchType),
            TransactionName = dto.TransactionName,
            Industry = dto.Industry,
            CompanyName = dto.CompanyName,
            Purpose = dto.Purpose,
            CompanyBusinessDescription = dto.CompanyBusinessDescription,
            ExclusionKeywords = dto.ExclusionKeywords,
            AiPrompt = dto.AiPrompt,
            CreatedAtUtc = DateTime.UtcNow
        };

        _store.AddSearch(search);
        return Task.FromResult(ToSavedSearchDetailDto(search));
    }

    public Task<SavedSearchDetailDto?> UpdateAsync(Guid id, UpdateSavedSearchDto dto, Guid? currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        var s = _store.GetSearchById(id);
        if (s == null) return Task.FromResult<SavedSearchDetailDto?>(null);
        if (!CanManage(s, currentUserId, isAdmin))
            throw new UnauthorizedAccessException("You are not allowed to manage this search.");

        if (dto.Name != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name cannot be empty.", nameof(dto));
            s.Name = dto.Name.Trim();
        }
        if (dto.FinancialYear != null)
        {
            if (string.IsNullOrWhiteSpace(dto.FinancialYear))
                throw new ArgumentException("FinancialYear cannot be empty.", nameof(dto));
            s.FinancialYear = dto.FinancialYear.Trim();
        }

        if (dto.TransactionName != null) s.TransactionName = dto.TransactionName;
        if (dto.Industry != null) s.Industry = dto.Industry;
        if (dto.CompanyName != null) s.CompanyName = dto.CompanyName;
        if (dto.Purpose != null) s.Purpose = dto.Purpose;
        if (dto.CompanyBusinessDescription != null) s.CompanyBusinessDescription = dto.CompanyBusinessDescription;
        if (dto.ExclusionKeywords != null) s.ExclusionKeywords = dto.ExclusionKeywords;
        if (dto.AiPrompt != null) s.AiPrompt = dto.AiPrompt;

        s.UpdatedAtUtc = DateTime.UtcNow;
        _store.UpdateSearch(s);
        return Task.FromResult<SavedSearchDetailDto?>(ToSavedSearchDetailDto(s));
    }

    public Task<bool> DeleteAsync(Guid id, Guid? currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var s = _store.GetSearchById(id);
        if (s == null) return Task.FromResult(false);
        if (!CanManage(s, currentUserId, isAdmin)) return Task.FromResult(false);
        return Task.FromResult(_store.RemoveSearch(id));
    }

    /// <summary>
    /// Returns a zip containing disposition output and reconciliation matrix (when available).
    /// Access: Standard Search = any user can download; Customized Search = only requestor or admin.
    /// </summary>
    public async Task<(Stream? Content, string? FileName)> DownloadSearchAsync(Guid id, Guid? requestorUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var s = _store.GetSearchById(id);
        if (s == null || !CanView(s, requestorUserId, isAdmin) || !s.BenchmarkingRequestId.HasValue)
            return (null, null);

        var requestId = s.BenchmarkingRequestId.Value;
        var (mainStream, mainName) = await _requestService.GetOutputForDownloadAsync(requestId, "main", cancellationToken);
        var (reconStream, reconName) = await _requestService.GetOutputForDownloadAsync(requestId, "recon", cancellationToken);

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

    private static bool IsStandardSearch(string value) =>
        string.Equals(value, "Standard Search", StringComparison.OrdinalIgnoreCase);

    private static bool IsCustomizedSearch(string value) =>
        string.Equals(value, "Customized Search", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "Customised Search", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeSearchType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return IsStandardSearch(raw) ? "Standard Search"
            : IsCustomizedSearch(raw) ? "Customized Search"
            : null;
    }

    private static bool CanView(SavedSearch s, Guid? userId, bool isAdmin)
    {
        if (IsStandardSearch(s.SearchType)) return true;
        if (isAdmin) return true;
        return s.RequestorUserId == userId;
    }

    private static bool CanManage(SavedSearch s, Guid? userId, bool isAdmin)
    {
        if (IsStandardSearch(s.SearchType)) return isAdmin;
        if (isAdmin) return true;
        return s.RequestorUserId == userId;
    }

    private static SavedSearchDetailDto ToSavedSearchDetailDto(SavedSearch s)
    {
        return new SavedSearchDetailDto
        {
            Id = s.Id,
            Name = s.Name,
            SearchType = s.SearchType,
            FinancialYear = s.FinancialYear,
            RequestorUserId = s.RequestorUserId,
            RequestorName = s.RequestorName,
            IsAdminManaged = s.IsAdminManaged,
            TransactionName = s.TransactionName,
            Industry = s.Industry,
            CompanyName = s.CompanyName,
            Purpose = s.Purpose,
            CompanyBusinessDescription = s.CompanyBusinessDescription,
            ExclusionKeywords = s.ExclusionKeywords,
            AiPrompt = s.AiPrompt
        };
    }
}
