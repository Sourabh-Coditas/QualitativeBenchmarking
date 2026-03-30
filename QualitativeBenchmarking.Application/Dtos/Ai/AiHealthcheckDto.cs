namespace KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

public record AiHealthcheckDto
{
    public string Status { get; init; } = null!;
    public string? LlmReply { get; init; }
}

