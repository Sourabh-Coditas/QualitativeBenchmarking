namespace KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

public record AiGeneratePromptResponseDto
{
    public string Prompt { get; init; } = null!;
}

