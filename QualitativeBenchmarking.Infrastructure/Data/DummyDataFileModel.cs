using KPMG.QualitativeBenchmarking.Domain.Entities;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Data;

/// <summary>
/// Root model for the dummy data JSON file.
/// </summary>
public class DummyDataFileModel
{
    public List<User> Users { get; set; } = new();
    public List<BenchmarkingRequest> Requests { get; set; } = new();
    public List<Feedback> Feedbacks { get; set; } = new();
}
