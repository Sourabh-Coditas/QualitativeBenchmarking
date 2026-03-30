namespace KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

public record AiDownloadRequestDto
{
    /// <summary>
    /// Either an absolute/relative path under AI_output (AI service validates) OR a relative download URL like "/download?path=...".
    /// </summary>
    public required string PathOrDownloadUrl { get; init; }
}

