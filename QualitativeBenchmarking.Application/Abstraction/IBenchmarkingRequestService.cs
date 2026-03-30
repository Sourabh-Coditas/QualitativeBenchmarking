using KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

namespace KPMG.QualitativeBenchmarking.Application.Abstraction;

public interface IBenchmarkingRequestService
{
    /// <param name="dto">Request data (user-provided business fields).</param>
    /// <param name="files">Current/previous/mapping upload file streams and names.</param>
    Task<BenchmarkingRequestDetailDto> CreateAsync(
        CreateBenchmarkingRequestDto dto,
        CreateBenchmarkingRequestFilesDto files,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<BenchmarkingRequestListItemDto> Items, int TotalCount)> GetListAsync(BenchmarkingRequestListFilterDto filter, CancellationToken cancellationToken = default);
    Task<BenchmarkingRequestDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <param name="currentUserId">Caller's user ID (for permission: Standard = admin only; Customized = owner or admin).</param>
    /// <param name="isAdmin">Whether the caller is an admin (can edit any request).</param>
    Task<BenchmarkingRequestDetailDto?> UpdateAsync(Guid id, UpdateBenchmarkingRequestDto dto, Guid? currentUserId = null, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid? currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<bool> ShareAsync(ShareRequestDto dto, CancellationToken cancellationToken = default);
    Task<bool> TriggerProcessingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ResubmitAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(Stream? Content, string? FileName)> GetOutputForDownloadAsync(Guid id, string outputType = "main", CancellationToken cancellationToken = default);
}
