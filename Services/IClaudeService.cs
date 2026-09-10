using CVMatchAI.API.Models;

namespace CVMatchAI.API.Services;

public interface IClaudeService
{
    /// <summary>
    /// Envía el CV y la oferta de trabajo a Gemini (Google AI Studio).
    /// Reintenta una vez si el JSON devuelto no es válido.
    /// </summary>
    Task<AnalysisResult> AnalyzeAsync(string cvText, string jobDescription);
}
