using System.Net.Http.Headers;
using System.Text;

namespace KPMG.QualitativeBenchmarking.Api.Tests.Helpers;

public static class MultipartRequestHelper
{
    /// <summary>
    /// Minimal valid Excel (xlsx) prefix - enough to pass extension check. Not a full workbook.
    /// </summary>
    private static readonly byte[] MinimalXlsx = new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00 };

    public static MultipartFormDataContent CreateBenchmarkingRequest(
        string benchmarkingName,
        string transactionName,
        string industry,
        string financialYear,
        string? searchType = "Standard Search",
        string? companyName = null,
        string? purpose = null,
        string companyBusinessDescription = "Test business description for benchmarking.",
        string exclusionKeywords = "test, exclude",
        string aiPrompt = "Test AI prompt for qualitative analysis.")
    {
        var content = new MultipartFormDataContent();

        content.Add(new StringContent(searchType ?? "Standard Search"), "searchType");
        content.Add(new StringContent(benchmarkingName), "benchmarkingName");
        content.Add(new StringContent(transactionName), "transactionName");
        content.Add(new StringContent(industry), "industry");
        content.Add(new StringContent(financialYear), "financialYear");
        content.Add(new StringContent(companyBusinessDescription), "companyBusinessDescription");
        content.Add(new StringContent(exclusionKeywords), "exclusionKeywords");
        content.Add(new StringContent(aiPrompt), "aiPrompt");

        if (companyName != null)
            content.Add(new StringContent(companyName), "companyName");
        if (purpose != null)
            content.Add(new StringContent(purpose), "purpose");

        var currentYearFile = new ByteArrayContent(MinimalXlsx);
        currentYearFile.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(currentYearFile, "currentYearFile", "current_year.xlsx");

        var columnMappingFile = new ByteArrayContent(MinimalXlsx);
        columnMappingFile.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(columnMappingFile, "columnMappingFile", "column_mapping.xlsx");

        return content;
    }
}
