using System.Net.Http.Headers;
using System.Net.Http.Json;
using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Ai;

namespace KPMG.QualitativeBenchmarking.Infrastructure.Services;

public class AiBenchmarkingService : IAiBenchmarkingService
{
    private readonly HttpClient _http;

    public AiBenchmarkingService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<AiServiceMetadataDto> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        var dto = await _http.GetFromJsonAsync<AiServiceMetadataDto>("/", cancellationToken);
        return dto ?? throw new InvalidOperationException("AI service returned empty metadata.");
    }

    public async Task<AiHealthcheckDto> HealthcheckAsync(CancellationToken cancellationToken = default)
    {
        var dto = await _http.GetFromJsonAsync<AiHealthcheckDto>("/healthcheck", cancellationToken);
        return dto ?? throw new InvalidOperationException("AI service returned empty healthcheck.");
    }

    public async Task<AiAnalyzeAllResponseDto> AnalyzeAllAsync(
        AiAnalyzeAllRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var form = new MultipartFormDataContent();

        var inputExcel = new StreamContent(request.InputExcel);
        inputExcel.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(inputExcel, "input_excel", request.InputExcelFileName);

        var mappingExcel = new StreamContent(request.MappingExcel);
        mappingExcel.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(mappingExcel, "mapping_excel", request.MappingExcelFileName);

        if (request.PrevYearExcel != null)
        {
            var prev = new StreamContent(request.PrevYearExcel);
            prev.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            form.Add(prev, "prev_year_excel", request.PrevYearExcelFileName ?? "prev_year.xlsx");
        }

        form.Add(new StringContent(request.TestedParty), "tested_party");
        if (!string.IsNullOrWhiteSpace(request.ExcludedWords))
            form.Add(new StringContent(request.ExcludedWords), "excluded_words");

        if (request.BatchSize.HasValue) form.Add(new StringContent(request.BatchSize.Value.ToString()), "batch_size");
        if (request.MaxConcurrency.HasValue) form.Add(new StringContent(request.MaxConcurrency.Value.ToString()), "max_concurrency");
        if (request.SheetConcurrency.HasValue) form.Add(new StringContent(request.SheetConcurrency.Value.ToString()), "sheet_concurrency");
        if (request.RequestTimeoutSeconds.HasValue) form.Add(new StringContent(request.RequestTimeoutSeconds.Value.ToString()), "request_timeout");
        if (request.StopOnFirstError.HasValue) form.Add(new StringContent(request.StopOnFirstError.Value.ToString().ToLowerInvariant()), "stop_on_first_error");

        using var response = await _http.PostAsync("/analyze-all", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<AiAnalyzeAllResponseDto>(cancellationToken: cancellationToken);
        return dto ?? throw new InvalidOperationException("AI service returned empty analyze-all response.");
    }

    public async Task<AiGeneratePromptResponseDto> GeneratePromptAsync(
        AiGeneratePromptRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.BusinessDescription))
            throw new ArgumentException("BusinessDescription is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ExclusionKeywords))
            throw new ArgumentException("ExclusionKeywords is required.", nameof(request));

        using var response = await _http.PostAsJsonAsync("/generate-prompt", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<AiGeneratePromptResponseDto>(cancellationToken: cancellationToken);
        return dto ?? throw new InvalidOperationException("AI service returned empty generate-prompt response.");
    }

    public async Task<(Stream Content, string FileName)> DownloadAsync(
        AiDownloadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.PathOrDownloadUrl))
            throw new ArgumentException("PathOrDownloadUrl is required.", nameof(request));

        var raw = request.PathOrDownloadUrl.Trim();
        var url = raw.StartsWith("/download", StringComparison.OrdinalIgnoreCase)
            ? raw
            : $"/download?path={Uri.EscapeDataString(raw)}";

        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentDisposition = response.Content.Headers.ContentDisposition;
        var fileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName ?? "output.xlsx";
        fileName = fileName.Trim('"');

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return (stream, fileName);
    }
}

