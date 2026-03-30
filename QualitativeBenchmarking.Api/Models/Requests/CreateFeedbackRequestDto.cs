namespace KPMG.QualitativeBenchmarking.Api.Models.Requests;

/// <summary>Body for POST /api/benchmarking-requests/{requestId}/feedback.</summary>
public sealed class CreateFeedbackRequestDto
{
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? Text { get; init; }
}
