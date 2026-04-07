using System.Security.Claims;
using CVMatchAI.API.DTOs;
using CVMatchAI.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVMatchAI.API.Controllers;

[ApiController]
[Route("api/analysis")]
[Authorize]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysis;

    public AnalysisController(IAnalysisService analysis) => _analysis = analysis;

    /// <summary>
    /// Crea un nuevo análisis: recibe el PDF y la descripción del trabajo,
    /// extrae el texto, llama a Claude y guarda el resultado.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB máximo
    public async Task<IActionResult> Create([FromForm] AnalysisRequestDto dto)
    {
        var userId = GetUserId();
        var result = await _analysis.CreateAsync(userId, dto);
        return Ok(result);
    }

    /// <summary>Lista el historial de análisis del usuario autenticado</summary>
    [HttpGet]
    public async Task<IActionResult> GetHistory()
    {
        var userId = GetUserId();
        var list   = await _analysis.GetHistoryAsync(userId);
        return Ok(list);
    }

    /// <summary>Obtiene el detalle completo de un análisis</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var result = await _analysis.GetByIdAsync(id, userId);
        return Ok(result);
    }

    /// <summary>Elimina un análisis del usuario</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        await _analysis.DeleteAsync(id, userId);
        return NoContent();
    }

    // ──────────────────── helper ────────────────────
    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
