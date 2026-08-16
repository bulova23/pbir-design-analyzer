namespace StoryAssessmentValidationExport;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            Console.Error.WriteLine("Usage: dotnet run --project service-dotnet/tools/StoryAssessmentValidationExport -- <reportPath> [outputDir]");
            return 1;
        }

        var reportPath = args[0];
        var outputDir = args.Length > 1 ? args[1] : null;

        try
        {
            var service = new StoryAssessmentValidationExportService();
            var exportDirectory = await service.ExportAsync(reportPath, outputDir).ConfigureAwait(false);
            Console.WriteLine($"Internal Validation Export written to: {exportDirectory}");
            Console.WriteLine("Not User-Facing Contract");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
