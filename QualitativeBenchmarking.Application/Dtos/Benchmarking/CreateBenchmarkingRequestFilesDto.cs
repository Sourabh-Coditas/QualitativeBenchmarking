namespace KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

public record CreateBenchmarkingRequestFilesDto
{
    public required Stream CurrentYearFile { get; init; }
    public required string CurrentYearFileName { get; init; }

    public Stream? PreviousYearFile { get; init; }
    public string? PreviousYearFileName { get; init; }

    public required Stream ColumnMappingFile { get; init; }
    public required string ColumnMappingFileName { get; init; }
}

