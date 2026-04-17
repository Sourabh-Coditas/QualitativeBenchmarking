using KPMG.QualitativeBenchmarking.Api.Models;
using KPMG.QualitativeBenchmarking.Api.Models.Requests;
using KPMG.QualitativeBenchmarking.Api.Models.Responses;
using KPMG.QualitativeBenchmarking.Application.Abstraction;
using KPMG.QualitativeBenchmarking.Application.Dtos.Ai;
using KPMG.QualitativeBenchmarking.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace KPMG.QualitativeBenchmarking.Api.Controllers;

[ApiController]
[Route("api/ai-prompts")]
public class AiPromptsController : ControllerBase
{
    private readonly IAiBenchmarkingService _ai;
    private readonly IPromptTemplateService _promptTemplates;
    private readonly IUserContext _userContext;

    public AiPromptsController(IAiBenchmarkingService ai, IPromptTemplateService promptTemplates, IUserContext userContext)
    {
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
        _promptTemplates = promptTemplates ?? throw new ArgumentNullException(nameof(promptTemplates));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<AiPromptTemplateDto>>> GetTemplates(CancellationToken cancellationToken)
    {
        var templates = await _promptTemplates.GetTemplatesAsync(cancellationToken);
        return Ok(templates.Select(t => new AiPromptTemplateDto
        {
            Id = t.Id.ToString(),
            Name = t.Name,
            Description = t.Description,
            PromptText = t.PromptText,
            IsDefault = t.IsDefault
        }).ToList());
    }

    [HttpPost("generate")]
    public async Task<ActionResult<GenerateAiPromptResponseDto>> Generate([FromBody] GenerateAiPromptRequestDto body, CancellationToken cancellationToken)
    {
        if (body == null) return BadRequest(new { error = "Body is required." });

        try
        {
            var (businessDescription, exclusionKeywords) =
                AiPromptInputValidator.ValidateAndNormalize(body.BusinessDescription, body.ExclusionKeywords);

            var generated = await _ai.GeneratePromptAsync(
                new AiGeneratePromptRequestDto
                {
                    BusinessDescription = businessDescription,
                    ExclusionKeywords = exclusionKeywords
                },
                cancellationToken);

            await _promptTemplates.SaveGeneratedPromptAsync(
                generated.Prompt,
                businessDescription,
                exclusionKeywords,
                _userContext.UserId,
                _userContext.Username,
                cancellationToken);

            var prompt = generated.Prompt;
            return Ok(new GenerateAiPromptResponseDto { AiPrompt = prompt });
        }
        catch (InputValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    [HttpPost("templates")]
    public async Task<ActionResult<AiPromptTemplateDto>> CreateTemplate([FromBody] CreateAiPromptTemplateRequestDto body, CancellationToken cancellationToken)
    {
        if (!_userContext.IsAdmin) return Forbid();
        if (body == null) return BadRequest(new { error = "Body is required." });

        try
        {
            var created = await _promptTemplates.CreateTemplateAsync(
                new CreatePromptTemplateDto
                {
                    Name = body.Name ?? "",
                    Description = body.Description,
                    PromptText = body.PromptText ?? "",
                    IsDefault = body.IsDefault
                },
                _userContext.UserId,
                _userContext.Username,
                cancellationToken);

            return CreatedAtAction(nameof(GetTemplates), new { id = created.Id }, new AiPromptTemplateDto
            {
                Id = created.Id.ToString(),
                Name = created.Name,
                Description = created.Description,
                PromptText = created.PromptText,
                IsDefault = created.IsDefault
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("templates/{id:guid}")]
    public async Task<ActionResult<AiPromptTemplateDto>> UpdateTemplate(Guid id, [FromBody] UpdateAiPromptTemplateRequestDto body, CancellationToken cancellationToken)
    {
        if (!_userContext.IsAdmin) return Forbid();
        if (body == null) return BadRequest(new { error = "Body is required." });

        try
        {
            var updated = await _promptTemplates.UpdateTemplateAsync(
                id,
                new UpdatePromptTemplateDto
                {
                    Name = body.Name,
                    Description = body.Description,
                    PromptText = body.PromptText,
                    IsDefault = body.IsDefault
                },
                cancellationToken);
            if (updated == null) return NotFound();
            return Ok(new AiPromptTemplateDto
            {
                Id = updated.Id.ToString(),
                Name = updated.Name,
                Description = updated.Description,
                PromptText = updated.PromptText,
                IsDefault = updated.IsDefault
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("templates/{id:guid}")]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken cancellationToken)
    {
        if (!_userContext.IsAdmin) return Forbid();
        var deleted = await _promptTemplates.DeleteTemplateAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
