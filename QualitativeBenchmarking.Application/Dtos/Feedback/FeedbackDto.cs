namespace KPMG.QualitativeBenchmarking.Application.Dtos.Feedback;

/// <summary>Feedback row for list/detail (view feedback flow).</summary>
public record FeedbackDto
{
    public Guid Id { get; init; }
    public Guid RequestId { get; init; }
    public Guid? UserId { get; init; }

    /// <summary>Who submitted (display name at submit time).</summary>
    public string SubmitterName { get; init; } = null!;

    /// <summary>Email at submit time when known from user profile.</summary>
    public string? SubmitterEmail { get; init; }

    /// <summary>Role at submit time (e.g. Admin, User).</summary>
    public string? Role { get; init; }

    /// <summary>When feedback was submitted (UTC).</summary>
    public DateTime SubmittedAtUtc { get; init; }

    public string Text { get; init; } = null!;
}
