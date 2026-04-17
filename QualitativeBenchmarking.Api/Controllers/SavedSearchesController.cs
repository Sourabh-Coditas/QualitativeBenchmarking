using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;
using KPMG.QualitativeBenchmarking.Api.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KPMG.QualitativeBenchmarking.Api.Controllers;

[ApiController]
[Route("api/saved-searches")]
public class SavedSearchesController : ControllerBase
{
    private readonly ISavedSearchService _service;
    private readonly IUserContext _userContext;

    public SavedSearchesController(ISavedSearchService service, IUserContext userContext)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    [HttpGet("standard")]
    public async Task<ActionResult<IReadOnlyList<SavedSearchItemDto>>> GetStandard(CancellationToken cancellationToken)
    {
        var list = await _service.GetStandardSearchesAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("customized")]
    public async Task<ActionResult<IReadOnlyList<SavedSearchItemDto>>> GetCustomized(CancellationToken cancellationToken)
    {
        var list = await _service.GetCustomizedSearchesAsync(_userContext.UserId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("standard/by-period")]
    public async Task<ActionResult<IReadOnlyList<SavedSearchPeriodGroupDto>>> GetStandardByPeriod(CancellationToken cancellationToken)
    {
        var groups = await _service.GetStandardSearchesGroupedByPeriodAsync(cancellationToken);
        return Ok(groups);
    }

    [HttpGet("customized/by-period")]
    public async Task<ActionResult<IReadOnlyList<SavedSearchPeriodGroupDto>>> GetCustomizedByPeriod(CancellationToken cancellationToken)
    {
        var groups = await _service.GetCustomizedSearchesGroupedByPeriodAsync(_userContext.UserId, cancellationToken);
        return Ok(groups);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SavedSearchDetailDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, _userContext.UserId, _userContext.IsAdmin, cancellationToken);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<SavedSearchDetailDto>> Create([FromBody] CreateSavedSearchRequestDto body, CancellationToken cancellationToken = default)
    {
        if (body == null) return BadRequest(new { error = "Body is required." });
        try
        {
            var dto = new CreateSavedSearchDto
            {
                Name = body.Name ?? "",
                SearchType = body.SearchType ?? "",
                FinancialYear = body.FinancialYear ?? "",
                TransactionName = body.TransactionName,
                Industry = body.Industry,
                CompanyName = body.CompanyName,
                Purpose = body.Purpose,
                CompanyBusinessDescription = body.CompanyBusinessDescription,
                ExclusionKeywords = body.ExclusionKeywords,
                AiPrompt = body.AiPrompt
            };
            var created = await _service.CreateAsync(dto, _userContext.UserId, _userContext.Username, _userContext.IsAdmin, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SavedSearchDetailDto>> Update(Guid id, [FromBody] UpdateSavedSearchRequestDto body, CancellationToken cancellationToken = default)
    {
        if (body == null) return BadRequest(new { error = "Body is required." });
        try
        {
            var dto = new UpdateSavedSearchDto
            {
                Name = body.Name,
                FinancialYear = body.FinancialYear,
                TransactionName = body.TransactionName,
                Industry = body.Industry,
                CompanyName = body.CompanyName,
                Purpose = body.Purpose,
                CompanyBusinessDescription = body.CompanyBusinessDescription,
                ExclusionKeywords = body.ExclusionKeywords,
                AiPrompt = body.AiPrompt
            };

            var updated = await _service.UpdateAsync(id, dto, _userContext.UserId, _userContext.IsAdmin, cancellationToken);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _service.DeleteAsync(id, _userContext.UserId, _userContext.IsAdmin, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken = default)
    {
        var (content, fileName) = await _service.DownloadSearchAsync(id, _userContext.UserId, _userContext.IsAdmin, cancellationToken);
        if (content == null) return NotFound();
        return File(content, "application/zip", fileName ?? "search.zip");
    }
}
