namespace KPMG.QualitativeBenchmarking.Application.Dtos.Feedback;

public record CreateFeedbackDto
{
    public Guid RequestId { get; init; }
    public Guid? UserId { get; init; }
    public string UserName { get; init; } = null!;
    public string Text { get; init; } = null!;
    /// <summary>Caller role at submit time (from auth context).</summary>
    public string? SubmitterRole { get; init; }
}

