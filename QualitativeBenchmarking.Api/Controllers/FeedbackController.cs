using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Feedback;
using KPMG.QualitativeBenchmarking.Api.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace KPMG.QualitativeBenchmarking.Api.Controllers;

[ApiController]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedback;
    private readonly IUserContext _userContext;

    public FeedbackController(IFeedbackService feedback, IUserContext userContext)
    {
        _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    [HttpPost("api/benchmarking-requests/{requestId:guid}/feedback")]
    public async Task<ActionResult<FeedbackDto>> Create(
        Guid requestId,
        [FromBody] CreateFeedbackRequestDto body,
        CancellationToken cancellationToken)
    {
        if (body == null) return BadRequest(new { error = "Body is required." });

        var dto = new CreateFeedbackDto
        {
            RequestId = requestId,
            UserId = _userContext.UserId ?? body.UserId,
            UserName = !string.IsNullOrWhiteSpace(_userContext.Username) ? _userContext.Username : (body.UserName ?? ""),
            Text = body.Text ?? "",
            SubmitterRole = _userContext.Role
        };

        try
        {
            var created = await _feedback.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetByRequest), new { requestId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("api/benchmarking-requests/{requestId:guid}/feedback")]
    public async Task<ActionResult<IReadOnlyList<FeedbackDto>>> GetByRequest(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var list = await _feedback.GetByRequestAsync(requestId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("api/feedback")]
    public async Task<ActionResult<IReadOnlyList<FeedbackDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        if (!_userContext.IsAdmin) return Forbid();
        var list = await _feedback.GetAllAsync(cancellationToken);
        return Ok(list);
    }
}
