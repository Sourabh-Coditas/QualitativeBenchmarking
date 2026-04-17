namespace KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

public record AiGeneratePromptRequestDto
{
    public string BusinessDescription { get; init; } = null!;
    public string ExclusionKeywords { get; init; } = null!;
}

