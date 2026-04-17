namespace KPMG.QualitativeBenchmarking.Domain.Entities;

public class SavedSearch
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string SearchType { get; set; } = null!; // Standard Search | Customized Search
    public string FinancialYear { get; set; } = null!;
    public Guid? RequestorUserId { get; set; }
    public string RequestorName { get; set; } = null!;
    public bool IsAdminManaged { get; set; }

    // Saved request parameters
    public string? TransactionName { get; set; }
    public string? Industry { get; set; }
    public string? CompanyName { get; set; }
    public string? Purpose { get; set; }
    public string? CompanyBusinessDescription { get; set; }
    public string? ExclusionKeywords { get; set; }
    public string? AiPrompt { get; set; }

    // Optional link to generated request for download flow.
    public Guid? BenchmarkingRequestId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

