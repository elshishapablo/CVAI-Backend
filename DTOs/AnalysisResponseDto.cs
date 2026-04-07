using CVMatchAI.API.Models;

namespace CVMatchAI.API.DTOs;

/// <summary>
/// Respuesta de autenticación con token y datos del usuario
/// </summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

/// <summary>
/// Datos públicos del usuario (sin contraseña)
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public int AnalysisUsedThisMonth { get; set; }
    public int AnalysisLimit { get; set; }   // -1 = ilimitado (pro)
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Respuesta completa de un análisis (incluye resultado de IA)
/// </summary>
public class AnalysisResponseDto
{
    public int Id { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public int CompatibilityScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public AnalysisResult Result { get; set; } = null!;
}

/// <summary>
/// Item del historial (versión reducida, sin el resultado completo)
/// </summary>
public class AnalysisListItemDto
{
    public int Id { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public int CompatibilityScore { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para actualizar el plan del usuario
/// </summary>
public class UpdatePlanDto
{
    public string Plan { get; set; } = string.Empty; // "free" | "pro"
}
