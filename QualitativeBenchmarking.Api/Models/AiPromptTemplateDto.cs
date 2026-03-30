namespace KPMG.QualitativeBenchmarking.Api.Models;

public sealed class AiPromptTemplateDto
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string PromptText { get; init; } = null!;
    public bool IsDefault { get; init; }
}
