namespace KPMG.QualitativeBenchmarking.Application.Validation;

public sealed class InputValidationException : Exception
{
    public IReadOnlyDictionary<string, string> Errors { get; }

    public InputValidationException(IReadOnlyDictionary<string, string> errors, string? message = null)
        : base(message ?? "Input validation failed.")
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }
}

