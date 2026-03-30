namespace KPMG.QualitativeBenchmarking.Infrastructure.Configuration;

public class AiServiceSettings
{
    public const string SectionName = "AiService";

    /// <summary>
    /// Base URL of the external FastAPI AI service, e.g. "https://example.azurewebsites.net".
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8000";

    public int TimeoutSeconds { get; set; } = 300;
}

