using CVMatchAI.API.DTOs;

namespace CVMatchAI.API.Services;

public interface IAnalysisService
{
    /// <summary>Crea un nuevo análisis: extrae PDF → llama Claude → guarda en DB</summary>
    Task<AnalysisResponseDto> CreateAsync(int userId, AnalysisRequestDto dto);

    /// <summary>Lista el historial de análisis del usuario</summary>
    Task<List<AnalysisListItemDto>> GetHistoryAsync(int userId);

    /// <summary>Obtiene el detalle completo de un análisis</summary>
    Task<AnalysisResponseDto> GetByIdAsync(int id, int userId);

    /// <summary>Elimina un análisis (solo si pertenece al usuario)</summary>
    Task DeleteAsync(int id, int userId);
}
