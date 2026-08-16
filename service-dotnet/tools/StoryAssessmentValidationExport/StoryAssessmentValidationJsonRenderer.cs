using System.Text.Json;

namespace StoryAssessmentValidationExport;

public static class StoryAssessmentValidationJsonRenderer
{
    public static string Render(StoryAssessmentValidationExportReport report)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        return JsonSerializer.Serialize(report, options);
    }
}
