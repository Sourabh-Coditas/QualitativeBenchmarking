namespace KPMG.QualitativeBenchmarking.Infrastructure.Configuration;

public class DummyDataFileSettings
{
    public const string SectionName = "DummyData";

    /// <summary>
    /// Full path to the JSON file (e.g. Data/dummy-data.json or absolute path).
    /// If not set, API should set it in Program.cs using ContentRootPath.
    /// </summary>
    public string FilePath { get; set; } = "Data/dummy-data.json";
}
