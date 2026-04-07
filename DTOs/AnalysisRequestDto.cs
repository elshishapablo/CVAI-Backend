using System.ComponentModel.DataAnnotations;

namespace CVMatchAI.API.DTOs;

public class AnalysisRequestDto
{
    [Required(ErrorMessage = "El archivo PDF es obligatorio")]
    public IFormFile PdfFile { get; set; } = null!;

    [Required(ErrorMessage = "La descripción del trabajo es obligatoria")]
    [MinLength(50, ErrorMessage = "La descripción debe tener al menos 50 caracteres")]
    public string JobDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "El título del cargo es obligatorio")]
    public string JobTitle { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;
}
