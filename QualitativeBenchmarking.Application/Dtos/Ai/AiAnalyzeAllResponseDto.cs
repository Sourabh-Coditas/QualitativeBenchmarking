namespace KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

public record AiAnalyzeAllResponseDto
{
    public string? MainReportPath { get; init; }
    public string? ReconReportPath { get; init; }
    public string? DownloadMain { get; init; }
    public string? DownloadRecon { get; init; }
}

