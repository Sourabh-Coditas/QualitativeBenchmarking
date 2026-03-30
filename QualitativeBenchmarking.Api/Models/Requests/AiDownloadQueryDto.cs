namespace KPMG.QualitativeBenchmarking.Api.Models.Requests;

/// <summary>Query parameters for GET /api/ai/download.</summary>
public sealed class AiDownloadQueryDto
{
    /// <summary>Path or download URL understood by the AI service.</summary>
    public string? PathOrUrl { get; set; }
}
