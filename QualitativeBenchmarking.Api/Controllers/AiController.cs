using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Ai;
using KPMG.QualitativeBenchmarking.Api.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KPMG.QualitativeBenchmarking.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiBenchmarkingService _ai;

    public AiController(IAiBenchmarkingService ai)
    {
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
    }

    [HttpGet]
    public async Task<ActionResult<AiServiceMetadataDto>> GetMetadata(CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _ai.GetMetadataAsync(cancellationToken);
            return Ok(dto);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    [HttpGet("healthcheck")]
    public async Task<ActionResult<AiHealthcheckDto>> Healthcheck(CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _ai.HealthcheckAsync(cancellationToken);
            return Ok(dto);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download(
        [FromQuery] AiDownloadQueryDto? query,
        CancellationToken cancellationToken)
    {
        query ??= new AiDownloadQueryDto();
        if (string.IsNullOrWhiteSpace(query.PathOrUrl))
            return BadRequest(new { error = "pathOrUrl is required." });

        try
        {
            var (content, fileName) = await _ai.DownloadAsync(
                new AiDownloadRequestDto { PathOrDownloadUrl = query.PathOrUrl },
                cancellationToken);

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }
}

