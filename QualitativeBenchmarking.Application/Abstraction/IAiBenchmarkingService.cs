using KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

namespace KPMG.QualitativeBenchmarking.Application.Abstraction;

public interface IAiBenchmarkingService
{
    Task<AiServiceMetadataDto> GetMetadataAsync(CancellationToken cancellationToken = default);
    Task<AiHealthcheckDto> HealthcheckAsync(CancellationToken cancellationToken = default);

    Task<AiAnalyzeAllResponseDto> AnalyzeAllAsync(
        AiAnalyzeAllRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(Stream Content, string FileName)> DownloadAsync(
        AiDownloadRequestDto request,
        CancellationToken cancellationToken = default);
}

