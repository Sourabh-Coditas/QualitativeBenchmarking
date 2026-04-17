namespace KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

public record UpdatePromptTemplateDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? PromptText { get; init; }
    public bool? IsDefault { get; init; }
}

