namespace KPMG.QualitativeBenchmarking.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsAdmin { get; set; }
}
