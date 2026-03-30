namespace KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

public record AiAnalyzeAllRequestDto
{
    public required Stream InputExcel { get; init; }
    public required string InputExcelFileName { get; init; }

    public required Stream MappingExcel { get; init; }
    public required string MappingExcelFileName { get; init; }

    public Stream? PrevYearExcel { get; init; }
    public string? PrevYearExcelFileName { get; init; }

    public required string TestedParty { get; init; }
    public string? ExcludedWords { get; init; }

    public int? BatchSize { get; init; }
    public int? MaxConcurrency { get; init; }
    public int? SheetConcurrency { get; init; }
    public int? RequestTimeoutSeconds { get; init; }
    public bool? StopOnFirstError { get; init; }
}

