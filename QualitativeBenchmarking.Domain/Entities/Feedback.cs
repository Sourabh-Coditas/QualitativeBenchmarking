namespace KPMG.QualitativeBenchmarking.Domain.Entities;

public class Feedback
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid? UserId { get; set; }
    /// <summary>Display name at submit time.</summary>
    public string UserName { get; set; } = null!;
    /// <summary>Email at submit time (from user profile when UserId matches).</summary>
    public string? SubmitterEmail { get; set; }
    /// <summary>Role at submit time (e.g. Admin, User).</summary>
    public string? SubmitterRole { get; set; }
    public string Text { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}

