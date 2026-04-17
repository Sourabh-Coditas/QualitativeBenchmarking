using System;
using System.Collections.Generic;
using KPMG.QualitativeBenchmarking.Application.Dtos.Benchmarking;

namespace KPMG.QualitativeBenchmarking.Application.Validation;

public sealed class BenchmarkingRequestInputValidator
{
    public sealed record CreateValidationResult(string NormalizedSearchType, string AiPrompt);

    public static CreateValidationResult ValidateAndNormalizeCreate(CreateBenchmarkingRequestDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var normalizedSearchType = NormalizeSearchType(dto.SearchType);
        if (normalizedSearchType == null)
            errors["searchType"] = "Search Type is required and must be either 'Standard Search' or 'Customised Search'.";

        ValidateRequiredMaxLen(errors, "benchmarkingName", dto.BenchmarkingName, 100);
        ValidateRequiredMaxLen(errors, "transactionName", dto.TransactionName, 100);
        ValidateRequiredMaxLen(errors, "industry", dto.Industry, 100);
        ValidateOptionalMaxLen(errors, "companyName", dto.CompanyName, 200);
        ValidateFinancialYear(errors, "financialYear", dto.FinancialYear);
        ValidateOptionalMaxLen(errors, "purpose", dto.Purpose, 50);
        ValidateRequiredMaxLen(errors, "companyBusinessDescription", dto.CompanyBusinessDescription, 2000);
        ValidateRequiredMaxLen(errors, "exclusionKeywords", dto.ExclusionKeywords, 1000);

        var aiPrompt = (dto.AiPrompt ?? "").Trim();

        if (string.IsNullOrWhiteSpace(aiPrompt))
            errors["aiPrompt"] = "AI Prompt is required.";
        if (!string.IsNullOrWhiteSpace(aiPrompt) && aiPrompt.Length > 5000)
            errors["aiPrompt"] = "AI Prompt must be 5000 characters or fewer.";

        if (errors.Count > 0)
            throw new InputValidationException(errors);

        return new CreateValidationResult(normalizedSearchType!, aiPrompt);
    }

    public static void ValidateUpdate(UpdateBenchmarkingRequestDto dto)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (dto.SearchType != null && NormalizeSearchType(dto.SearchType) == null)
            errors["searchType"] = "Search Type must be either 'Standard Search' or 'Customised Search'.";

        if (dto.BenchmarkingName != null) ValidateOptionalMaxLen(errors, "benchmarkingName", dto.BenchmarkingName, 100);
        if (dto.TransactionName != null) ValidateOptionalMaxLen(errors, "transactionName", dto.TransactionName, 100);
        if (dto.Industry != null) ValidateOptionalMaxLen(errors, "industry", dto.Industry, 100);
        if (dto.CompanyName != null) ValidateOptionalMaxLen(errors, "companyName", dto.CompanyName, 200);
        if (dto.Purpose != null) ValidateOptionalMaxLen(errors, "purpose", dto.Purpose, 50);
        if (dto.CompanyBusinessDescription != null) ValidateOptionalMaxLen(errors, "companyBusinessDescription", dto.CompanyBusinessDescription, 2000);
        if (dto.ExclusionKeywords != null) ValidateOptionalMaxLen(errors, "exclusionKeywords", dto.ExclusionKeywords, 1000);
        if (dto.AiPrompt != null) ValidateOptionalMaxLen(errors, "aiPrompt", dto.AiPrompt, 5000);

        if (dto.FinancialYear != null) ValidateFinancialYear(errors, "financialYear", dto.FinancialYear);

        if (errors.Count > 0)
            throw new InputValidationException(errors);
    }

    public static string? NormalizeSearchType(string? raw)
    {
        var v = (raw ?? "").Trim();
        if (string.IsNullOrWhiteSpace(v)) return null;

        if (string.Equals(v, "Standard Search", StringComparison.OrdinalIgnoreCase)) return "Standard Search";
        if (string.Equals(v, "Customised Search", StringComparison.OrdinalIgnoreCase)) return "Customised Search";
        if (string.Equals(v, "Customized Search", StringComparison.OrdinalIgnoreCase)) return "Customised Search";
        return null;
    }

    private static void ValidateRequiredMaxLen(Dictionary<string, string> errors, string key, string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = $"{key} is required.";
            return;
        }

        if (value.Trim().Length > maxLen)
            errors[key] = $"{key} must be {maxLen} characters or fewer.";
    }

    private static void ValidateOptionalMaxLen(Dictionary<string, string> errors, string key, string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (value.Trim().Length > maxLen)
            errors[key] = $"{key} must be {maxLen} characters or fewer.";
    }

    private static void ValidateFinancialYear(Dictionary<string, string> errors, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = "Financial Year is required.";
            return;
        }

        if (!TryParseFinancialYear(value.Trim(), out var startYear))
        {
            errors[key] = "Financial Year must be in format 'FY 20XX-XX'.";
            return;
        }

        var currentFyStart = GetCurrentFinancialYearStart(DateTime.UtcNow);
        if (startYear < 2016 || startYear > currentFyStart)
            errors[key] =
                $"Financial Year must be between FY 2016-17 and FY {currentFyStart}-{(currentFyStart + 1) % 100:D2}.";
    }

    private static bool TryParseFinancialYear(string value, out int startYear)
    {
        startYear = 0;
        if (!value.StartsWith("FY", StringComparison.OrdinalIgnoreCase)) return false;
        var rest = value[2..].Trim();
        var parts = rest.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out var year)) return false;
        if (year < 2000 || year > 2100) return false;
        if (parts[1].Length != 2) return false;
        if (!int.TryParse(parts[1], out var yy)) return false;
        if (yy < 0 || yy > 99) return false;
        if ((year + 1) % 100 != yy) return false;
        startYear = year;
        return true;
    }

    private static int GetCurrentFinancialYearStart(DateTime utcNow)
    {
        // Assumption: FY starts on Apr 1 (common in India).
        return utcNow.Month >= 4 ? utcNow.Year : utcNow.Year - 1;
    }
}

