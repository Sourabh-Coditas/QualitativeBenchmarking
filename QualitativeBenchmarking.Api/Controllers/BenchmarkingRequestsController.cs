using KPMG.QualitativeBenchmarking.Api.Models;
using KPMG.QualitativeBenchmarking.Api.Models.Requests;
using KPMG.QualitativeBenchmarking.Application.Validation;
using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;
using KPMG.QualitativeBenchmarking.Api.Validations;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace KPMG.QualitativeBenchmarking.Api.Controllers;

[ApiController]
[Route("api/benchmarking-requests")]
public class BenchmarkingRequestsController : ControllerBase
{
    private readonly IBenchmarkingRequestService _service;
    private readonly IUserContext _userContext;

    public BenchmarkingRequestsController(
        IBenchmarkingRequestService service,
        IUserContext userContext)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
    public async Task<ActionResult<BenchmarkingRequestDetailDto>> Create(
        [FromForm] CreateBenchmarkingRequestFormDto form,
        IFormFile? currentYearFile,
        IFormFile? previousYearFile,
        IFormFile? columnMappingFile,
        CancellationToken cancellationToken)
    {
        if (form == null)
            return BadRequest(new { error = "Form data is required." });

        var requestorName = !string.IsNullOrWhiteSpace(_userContext.Username)
            ? _userContext.Username
            : form.RequestorName?.Trim() ?? "User";
        var requestorUserId = _userContext.UserId ?? form.RequestorUserId;

        var fileError = BenchmarkingRequestFilesValidator.ValidateCreateFiles(currentYearFile, previousYearFile, columnMappingFile);
        if (fileError != null)
            return BadRequest(new { error = fileError });

        var dto = new CreateBenchmarkingRequestDto
        {
            SearchType = form.SearchType ?? "",
            BenchmarkingName = form.BenchmarkingName ?? "",
            TransactionName = form.TransactionName ?? "",
            Industry = form.Industry ?? "",
            CompanyName = form.CompanyName,
            FinancialYear = form.FinancialYear ?? "",
            Purpose = form.Purpose,
            CompanyBusinessDescription = form.CompanyBusinessDescription ?? "",
            ExclusionKeywords = form.ExclusionKeywords ?? "",
            AiPrompt = form.AiPrompt ?? "",
            RequestorName = requestorName,
            RequestorUserId = requestorUserId
        };

        try
        {
            await using var currentYearStream = currentYearFile!.OpenReadStream();
            await using var columnMappingStream = columnMappingFile!.OpenReadStream();
            Stream? previousYearStream = null;
            try
            {
                if (previousYearFile != null && previousYearFile.Length > 0)
                    previousYearStream = previousYearFile.OpenReadStream();

                var files = new CreateBenchmarkingRequestFilesDto
                {
                    CurrentYearFile = currentYearStream,
                    CurrentYearFileName = currentYearFile.FileName,
                    PreviousYearFile = previousYearStream,
                    PreviousYearFileName = previousYearFile?.FileName,
                    ColumnMappingFile = columnMappingStream,
                    ColumnMappingFileName = columnMappingFile.FileName
                };

                var result = await _service.CreateAsync(
                    dto,
                    files,
                    cancellationToken);

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            finally
            {
                if (previousYearStream != null)
                    await previousYearStream.DisposeAsync();
            }
        }
        catch (InputValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
        catch (TaskCanceledException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<(IReadOnlyList<BenchmarkingRequestListItemDto> Items, int TotalCount)>> GetList(
        [FromQuery] BenchmarkingRequestListQueryDto? query,
        CancellationToken cancellationToken = default)
    {
        query ??= new BenchmarkingRequestListQueryDto();
        var filter = new BenchmarkingRequestListFilterDto
        {
            BenchmarkingName = query.BenchmarkingName,
            TransactionName = query.TransactionName,
            FinancialYear = query.FinancialYear,
            SearchType = query.SearchType,
            RequestorName = query.RequestorName,
            Status = query.Status,
            MyRequestsOnly = query.MyRequestsOnly,
            RequestorUserId = query.MyRequestsOnly ? _userContext.UserId : null,
            Page = query.Page,
            PageSize = query.PageSize
        };
        var (items, total) = await _service.GetListAsync(filter, cancellationToken);
        return Ok(new { Items = items, TotalCount = total });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BenchmarkingRequestDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BenchmarkingRequestDetailDto>> Update(
        Guid id,
        [FromBody] UpdateBenchmarkingRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto == null) return BadRequest(new { error = "Body is required." });

        try
        {
            var result = await _service.UpdateAsync(id, dto, _userContext.UserId, _userContext.IsAdmin, cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (InputValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _service.DeleteAsync(id, _userContext.UserId, _userContext.IsAdmin, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("share")]
    public async Task<IActionResult> Share(
        [FromBody] ShareRequestDto dto,
        CancellationToken cancellationToken)
    {
        var ok = await _service.ShareAsync(dto, cancellationToken);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/process")]
    public async Task<IActionResult> TriggerProcessing(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _service.TriggerProcessingAsync(id, cancellationToken);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/resubmit")]
    public async Task<IActionResult> Resubmit(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _service.ResubmitAsync(id, cancellationToken);
        if (!ok) return BadRequest(new { error = "Request not found or not in Failed status." });
        return NoContent();
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadOutput(
        Guid id,
        [FromQuery] BenchmarkingRequestDownloadQueryDto? query,
        CancellationToken cancellationToken = default)
    {
        query ??= new BenchmarkingRequestDownloadQueryDto();
        var (content, fileName) = await _service.GetOutputForDownloadAsync(id, query.Type, cancellationToken);
        if (content == null || string.IsNullOrEmpty(fileName)) return NotFound();
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
