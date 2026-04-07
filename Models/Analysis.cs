namespace CVMatchAI.API.Models;

/// <summary>
/// Representa un análisis de compatibilidad CV vs oferta de trabajo
/// </summary>
public class Analysis
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Texto extraído del CV en PDF</summary>
    public string CvText { get; set; } = string.Empty;

    public string JobDescription { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;

    /// <summary>Score de compatibilidad del 0 al 100</summary>
    public int CompatibilityScore { get; set; }

    /// <summary>JSON completo con todo el resultado devuelto por Claude</summary>
    public string ResultJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegación
    public User User { get; set; } = null!;
}
