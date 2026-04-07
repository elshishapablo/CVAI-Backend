namespace CVMatchAI.API.Models;

/// <summary>
/// Estructura del resultado devuelto por Claude AI (deserializado desde JSON)
/// </summary>
public class AnalysisResult
{
    public int CompatibilityScore { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> MissingKeywords { get; set; } = new();
    public List<string> PresentKeywords { get; set; } = new();
    public List<SuggestionItem> Suggestions { get; set; } = new();
    public string CoverLetterIntro { get; set; } = string.Empty;
}

/// <summary>
/// Sugerencia de mejora con sección, texto original y versión mejorada
/// </summary>
public class SuggestionItem
{
    public string Section { get; set; } = string.Empty;
    public string Original { get; set; } = string.Empty;
    public string Improved { get; set; } = string.Empty;
}
