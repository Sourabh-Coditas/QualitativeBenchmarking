namespace KPMG.QualitativeBenchmarking.Api.Models.Requests;

public sealed class CreateAiPromptTemplateRequestDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? PromptText { get; init; }
    public bool IsDefault { get; init; }
}

