namespace KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

public record AiServiceMetadataDto
{
    public string Service { get; init; } = null!;
    public string Version { get; init; } = null!;
}

