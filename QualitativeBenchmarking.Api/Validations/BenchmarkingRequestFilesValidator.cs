using Microsoft.AspNetCore.Http;
using System.IO;

namespace KPMG.QualitativeBenchmarking.Api.Validations;

public static class BenchmarkingRequestFilesValidator
{
    private static readonly string[] AllowedExcelExtensions = { ".xls", ".xlsx" };

    public static string? ValidateCreateFiles(
        IFormFile? currentYearFile,
        IFormFile? previousYearFile,
        IFormFile? columnMappingFile)
    {
        if (currentYearFile == null || currentYearFile.Length == 0)
            return "Current Year Excel file is required.";

        if (columnMappingFile == null || columnMappingFile.Length == 0)
            return "Column Mappings Excel file is required.";

        if (!AllowedExcelExtensions.Contains(Path.GetExtension(currentYearFile.FileName).ToLowerInvariant()))
            return "Current Year file must be Excel (.xls or .xlsx).";

        if (previousYearFile != null && previousYearFile.Length > 0 &&
            !AllowedExcelExtensions.Contains(Path.GetExtension(previousYearFile.FileName).ToLowerInvariant()))
            return "Previous Year file must be Excel (.xls or .xlsx).";

        if (!AllowedExcelExtensions.Contains(Path.GetExtension(columnMappingFile.FileName).ToLowerInvariant()))
            return "Column Mappings file must be Excel (.xls or .xlsx).";

        return null;
    }
}

