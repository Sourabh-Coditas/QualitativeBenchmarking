namespace KPMG.QualitativeBenchmarking.Application.Utils;

public static class AiPromptComposer
{
    public static string Compose(string businessDescription, string exclusionKeywords)
    {
        return
            "You are performing qualitative benchmarking analysis.\n\n" +
            "## Tested party description\n" +
            businessDescription.Trim() + "\n\n" +
            "## Exclusion keywords\n" +
            "Treat these as exclusion criteria. Do not include these keywords in the prompt text.\n" +
            exclusionKeywords.Trim() + "\n\n" +
            "## Instructions\n" +
            "- Use the tested party description to guide analysis.\n" +
            "- Apply exclusions strictly.\n";
    }
}

