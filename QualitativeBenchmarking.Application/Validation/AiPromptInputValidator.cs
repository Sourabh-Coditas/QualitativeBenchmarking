namespace KPMG.QualitativeBenchmarking.Application.Validation;

public static class AiPromptInputValidator
{
    public static (string BusinessDescription, string ExclusionKeywords) ValidateAndNormalize(string? businessDescription, string? exclusionKeywords)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(businessDescription))
            errors["businessDescription"] = "BusinessDescription is required.";

        if (string.IsNullOrWhiteSpace(exclusionKeywords))
            errors["exclusionKeywords"] = "ExclusionKeywords is required.";

        if (errors.Count > 0)
            throw new InputValidationException(errors);

        return (businessDescription!.Trim(), exclusionKeywords!.Trim());
    }
}

