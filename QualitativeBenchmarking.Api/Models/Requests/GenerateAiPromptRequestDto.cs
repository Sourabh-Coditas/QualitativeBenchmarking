namespace KPMG.QualitativeBenchmarking.Api.Models.Requests;

/// <summary>Body for POST /api/ai-prompts/generate.</summary>
public sealed class GenerateAiPromptRequestDto
{
    public string? BusinessDescription { get; init; }
    public string? ExclusionKeywords { get; init; }
}
