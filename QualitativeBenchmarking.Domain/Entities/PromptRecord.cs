namespace KPMG.QualitativeBenchmarking.Domain.Entities;

public class PromptRecord
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string PromptText { get; set; } = null!;
    public bool IsDefault { get; set; }
    public bool IsManagedTemplate { get; set; }
    public string? BusinessDescription { get; set; }
    public string? ExclusionKeywords { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

