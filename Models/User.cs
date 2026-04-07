namespace CVMatchAI.API.Models;

/// <summary>
/// Representa un usuario registrado en la plataforma
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Plan del usuario: "free" o "pro"</summary>
    public string Plan { get; set; } = "free";

    /// <summary>Número de análisis usados en el mes actual</summary>
    public int AnalysisUsedThisMonth { get; set; } = 0;

    /// <summary>Fecha del último reset del contador mensual</summary>
    public DateTime LastResetDate { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Analysis> Analyses { get; set; } = new List<Analysis>();
}
