namespace KPMG.QualitativeBenchmarking.Api.Models.Requests;

/// <summary>Query parameters for GET /api/benchmarking-requests/{id}/download.</summary>
public sealed class BenchmarkingRequestDownloadQueryDto
{
    /// <summary>Output type: "main" or "recon".</summary>
    public string Type { get; set; } = "main";
}
