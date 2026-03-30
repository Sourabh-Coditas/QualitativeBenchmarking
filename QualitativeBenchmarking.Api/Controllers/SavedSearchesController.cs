using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;
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

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken = default)
    {
        var (content, fileName) = await _service.DownloadSearchAsync(id, _userContext.UserId, _userContext.IsAdmin, cancellationToken);
        if (content == null) return NotFound();
        return File(content, "application/zip", fileName ?? "search.zip");
    }
}
