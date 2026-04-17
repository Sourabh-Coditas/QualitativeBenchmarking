namespace KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

public record CreatePromptTemplateDto
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string PromptText { get; init; } = null!;
    public bool IsDefault { get; init; }
}

