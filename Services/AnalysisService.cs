using System.Text.Json;
using CVMatchAI.API.Data;
using CVMatchAI.API.DTOs;
using CVMatchAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CVMatchAI.API.Services;

public class AnalysisService : IAnalysisService
{
    private readonly AppDbContext _db;
    private readonly IPdfService _pdf;
    private readonly IClaudeService _claude;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AnalysisService(AppDbContext db, IPdfService pdf, IClaudeService claude)
    {
        _db     = db;
        _pdf    = pdf;
        _claude = claude;
    }

    public async Task<AnalysisResponseDto> CreateAsync(int userId, AnalysisRequestDto dto)
    {
        // Obtener usuario y verificar límite de plan
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        AuthService.ResetMonthlyCountIfNeeded(user);

        if (user.Plan == "free" && user.AnalysisUsedThisMonth >= 3)
            throw new InvalidOperationException(
                "Has alcanzado el límite de 3 análisis gratuitos este mes. " +
                "Actualiza a Pro para análisis ilimitados.");

        // Extraer texto del PDF
        string cvText;
        try
        {
            using var stream = dto.PdfFile.OpenReadStream();
            cvText = _pdf.ExtractText(stream);
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                "No se pudo leer el PDF. Asegúrate de que no esté protegido con contraseña.", ex);
        }

        if (string.IsNullOrWhiteSpace(cvText) || cvText.Length < 50)
            throw new ArgumentException("El PDF parece estar vacío o no contiene texto extraíble.");

        // Llamar a Claude AI
        var aiResult = await _claude.AnalyzeAsync(cvText, dto.JobDescription);

        // Guardar en base de datos
        var analysis = new Analysis
        {
            UserId             = userId,
            CvText             = cvText[..Math.Min(cvText.Length, 10000)],
            JobDescription     = dto.JobDescription,
            JobTitle           = dto.JobTitle,
            Company            = dto.Company,
            CompatibilityScore = aiResult.CompatibilityScore,
            ResultJson         = JsonSerializer.Serialize(aiResult, JsonOpts),
            CreatedAt          = DateTime.UtcNow
        };

        _db.Analyses.Add(analysis);

        // Incrementar contador mensual
        user.AnalysisUsedThisMonth++;
        await _db.SaveChangesAsync();

        return MapToResponse(analysis, aiResult);
    }

    public async Task<List<AnalysisListItemDto>> GetHistoryAsync(int userId)
    {
        return await _db.Analyses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AnalysisListItemDto
            {
                Id                 = a.Id,
                JobTitle           = a.JobTitle,
                Company            = a.Company,
                CompatibilityScore = a.CompatibilityScore,
                CreatedAt          = a.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<AnalysisResponseDto> GetByIdAsync(int id, int userId)
    {
        var analysis = await _db.Analyses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw new KeyNotFoundException("Análisis no encontrado.");

        var result = JsonSerializer.Deserialize<AnalysisResult>(analysis.ResultJson, JsonOpts)
                     ?? new AnalysisResult();

        return MapToResponse(analysis, result);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        var analysis = await _db.Analyses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw new KeyNotFoundException("Análisis no encontrado.");

        _db.Analyses.Remove(analysis);
        await _db.SaveChangesAsync();
    }

    // ──────────────────── helpers privados ────────────────────

    private static AnalysisResponseDto MapToResponse(Analysis a, AnalysisResult result) => new()
    {
        Id                 = a.Id,
        JobTitle           = a.JobTitle,
        Company            = a.Company,
        CompatibilityScore = a.CompatibilityScore,
        CreatedAt          = a.CreatedAt,
        Result             = result
    };
}
