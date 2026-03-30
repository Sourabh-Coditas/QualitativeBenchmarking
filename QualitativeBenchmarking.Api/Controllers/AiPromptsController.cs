using KPMG.QualitativeBenchmarking.Api.Models;
using KPMG.QualitativeBenchmarking.Api.Models.Requests;
using KPMG.QualitativeBenchmarking.Api.Models.Responses;
using KPMG.QualitativeBenchmarking.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace KPMG.QualitativeBenchmarking.Api.Controllers;

[ApiController]
[Route("api/ai-prompts")]
public class AiPromptsController : ControllerBase
{
    [HttpGet("templates")]
    public ActionResult<IReadOnlyList<AiPromptTemplateDto>> GetTemplates()
    {
        // Pre-loaded library. Can be moved to DB/config later.
        var templates = new List<AiPromptTemplateDto>
        {
            new()
            {
                Id = "default_v1",
                Name = "Default (v1)",
                Description = "Structured prompt built from business description + exclusions.",
                PromptText = "Auto-generated",
                IsDefault = true
            },
            new()
            {
                Id = "tp_focus",
                Name = "TP Focused",
                Description = "Emphasize tested party profile and key differentiators.",
                PromptText =
                    "Focus on tested party description. Highlight products/services, key functions, risks, and business model. Apply exclusions strictly.",
                IsDefault = false
            },
            new()
            {
                Id = "strict_exclusions",
                Name = "Strict exclusions",
                Description = "Explicitly call out exclusion handling.",
                PromptText =
                    "Use tested party description as primary context. Treat exclusions as hard filters. Do not include excluded words in any reasoning output.",
                IsDefault = false
            }
        };

        return Ok(templates);
    }

    [HttpPost("generate")]
    public ActionResult<GenerateAiPromptResponseDto> Generate([FromBody] GenerateAiPromptRequestDto body)
    {
        if (body == null) return BadRequest(new { error = "Body is required." });

        try
        {
            var prompt = AiPromptInputValidator.ValidateAndCompose(body.BusinessDescription, body.ExclusionKeywords);
            return Ok(new GenerateAiPromptResponseDto { AiPrompt = prompt });
        }
        catch (InputValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }
}
