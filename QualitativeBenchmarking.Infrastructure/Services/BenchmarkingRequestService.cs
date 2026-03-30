using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;
using KPMG.QualitativeBenchmarking.Application.Dtos.Ai;
using KPMG.QualitativeBenchmarking.Domain.Entities;
using KPMG.QualitativeBenchmarking.Application.Validation;
using System.IO;
using KPMG.QualitativeBenchmarking.Infrastructure.Data;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Services;

/// <summary>
/// Request creation is interdependent with the AI call.
/// If AI analysis fails, the persisted request and stored files are rolled back.
/// </summary>
public class BenchmarkingRequestService : IBenchmarkingRequestService
{
    private readonly DummyDataStore _store;
    private readonly IFileStorageService _fileStorage;
    private readonly IAiBenchmarkingService _ai;

    public BenchmarkingRequestService(
        DummyDataStore store,
        IFileStorageService fileStorage,
        IAiBenchmarkingService ai)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
    }

    public Task<BenchmarkingRequestDetailDto> CreateAsync(
        CreateBenchmarkingRequestDto dto,
        CreateBenchmarkingRequestFilesDto files,
        CancellationToken cancellationToken = default)
    {
        if (files is null) throw new ArgumentNullException(nameof(files));

        var validated = BenchmarkingRequestInputValidator.ValidateAndNormalizeCreate(dto);
        var requestFolderName = BuildRequestFolderName(dto.RequestorName);

        return CreateWithFilesAndAnalyzeAsync(dto, validated, files, requestFolderName, cancellationToken);
    }

    private async Task<BenchmarkingRequestDetailDto> CreateWithFilesAndAnalyzeAsync(
        CreateBenchmarkingRequestDto dto,
        BenchmarkingRequestInputValidator.CreateValidationResult validated,
        CreateBenchmarkingRequestFilesDto files,
        string requestFolderName,
        CancellationToken cancellationToken)
    {
        string? currentYearFilePath = null;
        string? previousYearFilePath = null;
        string? columnMappingFilePath = null;

        try
        {
            currentYearFilePath = await _fileStorage.StoreBenchmarkingFileAsync(
                files.CurrentYearFile,
                files.CurrentYearFileName,
                requestFolderName,
                cancellationToken);

            if (files.PreviousYearFile != null && !string.IsNullOrWhiteSpace(files.PreviousYearFileName))
            {
                previousYearFilePath = await _fileStorage.StoreBenchmarkingFileAsync(
                    files.PreviousYearFile,
                    files.PreviousYearFileName,
                    requestFolderName,
                    cancellationToken);
            }

            columnMappingFilePath = await _fileStorage.StoreBenchmarkingFileAsync(
                files.ColumnMappingFile,
                files.ColumnMappingFileName,
                requestFolderName,
                cancellationToken);
        }
        catch
        {
            await DeleteIfExistsAsync(currentYearFilePath, cancellationToken);
            await DeleteIfExistsAsync(previousYearFilePath, cancellationToken);
            await DeleteIfExistsAsync(columnMappingFilePath, cancellationToken);
            throw;
        }

        var entity = new BenchmarkingRequest
        {
            Id = Guid.NewGuid(),
            SearchType = validated.NormalizedSearchType,
            BenchmarkingName = dto.BenchmarkingName,
            TransactionName = dto.TransactionName,
            Industry = dto.Industry,
            CompanyName = dto.CompanyName,
            FinancialYear = dto.FinancialYear,
            Purpose = dto.Purpose,
            CompanyBusinessDescription = dto.CompanyBusinessDescription,
            ExclusionKeywords = dto.ExclusionKeywords,
            AiPrompt = validated.AiPrompt,
            RequestorName = dto.RequestorName,
            RequestorUserId = dto.RequestorUserId,
            CurrentYearFilePath = currentYearFilePath!,
            PreviousYearFilePath = previousYearFilePath,
            ColumnMappingFilePath = columnMappingFilePath!,
            Status = "Submitted",
            CreatedAtUtc = DateTime.UtcNow
        };
        _store.AddRequest(entity);

        return await CreateAndAnalyzeAsync(
            entity,
            currentYearFilePath!,
            previousYearFilePath,
            columnMappingFilePath!,
            cancellationToken);
    }

    private async Task<BenchmarkingRequestDetailDto> CreateAndAnalyzeAsync(
        BenchmarkingRequest entity,
        string currentYearFilePath,
        string? previousYearFilePath,
        string columnMappingFilePath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var inputExcel = File.OpenRead(currentYearFilePath);
            await using var mappingExcel = File.OpenRead(columnMappingFilePath);

            Stream? prevYearExcel = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(previousYearFilePath))
                    prevYearExcel = File.OpenRead(previousYearFilePath);

                var aiRequest = new AiAnalyzeAllRequestDto
                {
                    InputExcel = inputExcel,
                    InputExcelFileName = Path.GetFileName(currentYearFilePath),
                    MappingExcel = mappingExcel,
                    MappingExcelFileName = Path.GetFileName(columnMappingFilePath),
                    PrevYearExcel = prevYearExcel,
                    PrevYearExcelFileName = string.IsNullOrWhiteSpace(previousYearFilePath)
                        ? null
                        : Path.GetFileName(previousYearFilePath),
                    TestedParty = entity.CompanyBusinessDescription,
                    ExcludedWords = string.IsNullOrWhiteSpace(entity.ExclusionKeywords) ? null : entity.ExclusionKeywords
                };

                var aiResponse = await _ai.AnalyzeAllAsync(aiRequest, cancellationToken);

                entity.Status = "Generated";
                entity.AiMainReportPath = aiResponse.MainReportPath;
                entity.AiReconReportPath = aiResponse.ReconReportPath;
                entity.AiDownloadMain = aiResponse.DownloadMain;
                entity.AiDownloadRecon = aiResponse.DownloadRecon;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                _store.UpdateRequest(entity);

                return MapToDetail(entity);
            }
            finally
            {
                prevYearExcel?.Dispose();
            }
        }
        catch
        {
            // Roll back the persisted request and stored files so create is "all-or-nothing".
            _store.RemoveRequest(entity.Id);

            await _fileStorage.DeleteAsync(currentYearFilePath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(previousYearFilePath))
                await _fileStorage.DeleteAsync(previousYearFilePath, cancellationToken);
            await _fileStorage.DeleteAsync(columnMappingFilePath, cancellationToken);

            throw;
        }
    }

    private async Task DeleteIfExistsAsync(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        await _fileStorage.DeleteAsync(path, cancellationToken);
    }

    private static string BuildRequestFolderName(string requestorName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", (requestorName ?? "").Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
        var safeName = string.IsNullOrEmpty(sanitized) ? "Uploads" : sanitized;
        return safeName + "_" + Guid.NewGuid().ToString("N");
    }

    public Task<(IReadOnlyList<BenchmarkingRequestListItemDto> Items, int TotalCount)> GetListAsync(BenchmarkingRequestListFilterDto filter, CancellationToken cancellationToken = default)
    {
        var all = _store.GetAllRequests().AsEnumerable();
        if (filter.MyRequestsOnly && filter.RequestorUserId.HasValue)
            all = all.Where(r => r.RequestorUserId == filter.RequestorUserId);
        if (!string.IsNullOrWhiteSpace(filter.BenchmarkingName))
            all = all.Where(r => r.BenchmarkingName.Contains(filter.BenchmarkingName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.TransactionName))
            all = all.Where(r => r.TransactionName.Contains(filter.TransactionName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.FinancialYear))
            all = all.Where(r => r.FinancialYear == filter.FinancialYear);
        if (!string.IsNullOrWhiteSpace(filter.SearchType))
            all = all.Where(r => r.SearchType == filter.SearchType);
        if (!string.IsNullOrWhiteSpace(filter.RequestorName))
            all = all.Where(r => r.RequestorName.Contains(filter.RequestorName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Status))
            all = all.Where(r => r.Status == filter.Status);

        var list = all.OrderByDescending(r => r.CreatedAtUtc).ToList();
        var total = list.Count;
        var items = list
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select((r, i) => new BenchmarkingRequestListItemDto
            {
                SrNo = (filter.Page - 1) * filter.PageSize + i + 1,
                Id = r.Id,
                BenchmarkingName = r.BenchmarkingName,
                TransactionName = r.TransactionName,
                FinancialYear = r.FinancialYear,
                SearchType = r.SearchType,
                RequestorName = r.RequestorName,
                CreatedAtUtc = r.CreatedAtUtc,
                Status = r.Status
            })
            .ToList();
        return Task.FromResult<(IReadOnlyList<BenchmarkingRequestListItemDto>, int)>((items, total));
    }

    public Task<BenchmarkingRequestDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var r = _store.GetRequestById(id);
        return Task.FromResult(r != null ? MapToDetail(r) : null);
    }

    public Task<BenchmarkingRequestDetailDto?> UpdateAsync(Guid id, UpdateBenchmarkingRequestDto dto, Guid? currentUserId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        BenchmarkingRequestInputValidator.ValidateUpdate(dto);

        var r = _store.GetRequestById(id);
        if (r == null) return Task.FromResult<BenchmarkingRequestDetailDto?>(null);
        if (string.Equals(r.Status, "InProcess", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Request is currently being processed and cannot be edited.");

        // Spec: Standard Search = only Admins can edit; Customized Search = owner or admin can edit.
        var isStandard = string.Equals(r.SearchType, "Standard Search", StringComparison.OrdinalIgnoreCase);
        if (isStandard && !isAdmin)
            throw new InvalidOperationException("Only administrators can edit Standard Search requests.");
        if (!isStandard && !isAdmin && r.RequestorUserId != currentUserId)
            throw new InvalidOperationException("You can only edit your own Customized Search requests.");

        var hasCoreChange =
            (dto.CompanyBusinessDescription != null && dto.CompanyBusinessDescription != r.CompanyBusinessDescription) ||
            (dto.ExclusionKeywords != null && dto.ExclusionKeywords != r.ExclusionKeywords) ||
            (dto.AiPrompt != null && dto.AiPrompt != r.AiPrompt);

        if (hasCoreChange)
        {
            var entity = new BenchmarkingRequest
            {
                Id = Guid.NewGuid(),
                SearchType = BenchmarkingRequestInputValidator.NormalizeSearchType(dto.SearchType) ?? r.SearchType,
                BenchmarkingName = dto.BenchmarkingName ?? r.BenchmarkingName,
                TransactionName = dto.TransactionName ?? r.TransactionName,
                Industry = dto.Industry ?? r.Industry,
                CompanyName = dto.CompanyName ?? r.CompanyName,
                FinancialYear = dto.FinancialYear ?? r.FinancialYear,
                Purpose = dto.Purpose ?? r.Purpose,
                CompanyBusinessDescription = dto.CompanyBusinessDescription ?? r.CompanyBusinessDescription,
                ExclusionKeywords = dto.ExclusionKeywords ?? r.ExclusionKeywords,
                AiPrompt = dto.AiPrompt ?? r.AiPrompt,
                CurrentYearFilePath = r.CurrentYearFilePath,
                PreviousYearFilePath = r.PreviousYearFilePath,
                ColumnMappingFilePath = r.ColumnMappingFilePath,
                Status = "Submitted",
                RequestorName = r.RequestorName,
                RequestorUserId = r.RequestorUserId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _store.AddRequest(entity);
            return Task.FromResult<BenchmarkingRequestDetailDto?>(MapToDetail(entity));
        }

        if (dto.SearchType != null) r.SearchType = BenchmarkingRequestInputValidator.NormalizeSearchType(dto.SearchType) ?? r.SearchType;
        if (dto.BenchmarkingName != null) r.BenchmarkingName = dto.BenchmarkingName;
        if (dto.TransactionName != null) r.TransactionName = dto.TransactionName;
        if (dto.Industry != null) r.Industry = dto.Industry;
        if (dto.CompanyName != null) r.CompanyName = dto.CompanyName;
        if (dto.FinancialYear != null) r.FinancialYear = dto.FinancialYear;
        if (dto.Purpose != null) r.Purpose = dto.Purpose;

        r.UpdatedAtUtc = DateTime.UtcNow;
        _store.UpdateRequest(r);
        return Task.FromResult<BenchmarkingRequestDetailDto?>(MapToDetail(r));
    }

    public Task<bool> DeleteAsync(Guid id, Guid? currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var r = _store.GetRequestById(id);
        if (r == null) return Task.FromResult(false);
        if (!isAdmin && r.RequestorUserId != currentUserId) return Task.FromResult(false);
        return Task.FromResult(_store.RemoveRequest(id));
    }

    public Task<bool> ShareAsync(ShareRequestDto dto, CancellationToken cancellationToken = default)
    {
        var r = _store.GetRequestById(dto.RequestId);
        if (r == null) return Task.FromResult(false);
        // Dummy: no actual sharing implementation; just acknowledge.
        return Task.FromResult(true);
    }

    /// <summary>Marks the request as eligible for processing. Queuing and execution are done by the AI service.</summary>
    public Task<bool> TriggerProcessingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var r = _store.GetRequestById(id);
        if (r == null) return Task.FromResult(false);
        return Task.FromResult(true);
    }

    public Task<bool> ResubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var r = _store.GetRequestById(id);
        if (r == null) return Task.FromResult(false);
        if (!string.Equals(r.Status, "Failed", StringComparison.OrdinalIgnoreCase)) return Task.FromResult(false);

        r.Status = "Submitted";
        r.ProcessingError = null;
        r.OutputFilePath = null;
        r.ReconOutputFilePath = null;
        r.AiMainReportPath = null;
        r.AiReconReportPath = null;
        r.AiDownloadMain = null;
        r.AiDownloadRecon = null;
        r.UpdatedAtUtc = DateTime.UtcNow;
        _store.UpdateRequest(r);

        return Task.FromResult(true);
    }

    public async Task<(Stream? Content, string? FileName)> GetOutputForDownloadAsync(
        Guid id,
        string outputType = "main",
        CancellationToken cancellationToken = default)
    {
        var r = _store.GetRequestById(id);
        if (r == null) return (null, null);

        var isRecon = string.Equals(outputType, "recon", StringComparison.OrdinalIgnoreCase);

        var localPath = isRecon ? r.ReconOutputFilePath : r.OutputFilePath;
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            var stream = await _fileStorage.GetAsync(localPath, cancellationToken);
            if (stream != null)
                return (stream, Path.GetFileName(localPath) ?? (isRecon ? "recon.xlsx" : "output.xlsx"));
        }

        var downloadUrl = isRecon ? r.AiDownloadRecon : r.AiDownloadMain;
        var reportPath = isRecon ? r.AiReconReportPath : r.AiMainReportPath;
        var pathOrUrl = !string.IsNullOrWhiteSpace(downloadUrl) ? downloadUrl : reportPath;
        if (string.IsNullOrWhiteSpace(pathOrUrl)) return (null, null);

        var (content, fileName) = await _ai.DownloadAsync(
            new AiDownloadRequestDto { PathOrDownloadUrl = pathOrUrl },
            cancellationToken);
        return (content, fileName);
    }

    private static BenchmarkingRequestDetailDto MapToDetail(BenchmarkingRequest r)
    {
        var canDownloadMain = string.Equals(r.Status, "Generated", StringComparison.OrdinalIgnoreCase) &&
                              (!string.IsNullOrWhiteSpace(r.OutputFilePath) ||
                               !string.IsNullOrWhiteSpace(r.AiDownloadMain) ||
                               !string.IsNullOrWhiteSpace(r.AiMainReportPath));
        var canDownloadRecon = string.Equals(r.Status, "Generated", StringComparison.OrdinalIgnoreCase) &&
                               (!string.IsNullOrWhiteSpace(r.ReconOutputFilePath) ||
                                !string.IsNullOrWhiteSpace(r.AiDownloadRecon) ||
                                !string.IsNullOrWhiteSpace(r.AiReconReportPath));

        return new BenchmarkingRequestDetailDto
        {
            Id = r.Id,
            SearchType = r.SearchType,
            BenchmarkingName = r.BenchmarkingName,
            TransactionName = r.TransactionName,
            Industry = r.Industry,
            CompanyName = r.CompanyName,
            FinancialYear = r.FinancialYear,
            Purpose = r.Purpose,
            CompanyBusinessDescription = r.CompanyBusinessDescription,
            ExclusionKeywords = r.ExclusionKeywords,
            AiPrompt = r.AiPrompt,
            CurrentYearFileName = r.CurrentYearFilePath != null ? Path.GetFileName(r.CurrentYearFilePath) : null,
            PreviousYearFileName = r.PreviousYearFilePath != null ? Path.GetFileName(r.PreviousYearFilePath) : null,
            ColumnMappingFileName = r.ColumnMappingFilePath != null ? Path.GetFileName(r.ColumnMappingFilePath) : null,
            Status = r.Status,
            DownloadMain = canDownloadMain ? $"/api/benchmarking-requests/{r.Id}/download?type=main" : null,
            DownloadRecon = canDownloadRecon ? $"/api/benchmarking-requests/{r.Id}/download?type=recon" : null,
            ProcessingError = r.ProcessingError,
            RequestorName = r.RequestorName,
            CreatedAtUtc = r.CreatedAtUtc,
            UpdatedAtUtc = r.UpdatedAtUtc
        };
    }

}
