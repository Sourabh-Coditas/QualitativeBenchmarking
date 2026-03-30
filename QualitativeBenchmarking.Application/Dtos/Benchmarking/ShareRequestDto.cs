namespace KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

public record ShareRequestDto
{
    public Guid RequestId { get; init; }
    public string Email { get; init; } = null!;
}
