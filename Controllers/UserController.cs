using System.Security.Claims;
using CVMatchAI.API.Data;
using CVMatchAI.API.DTOs;
using CVMatchAI.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVMatchAI.API.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _auth;

    public UserController(AppDbContext db, IAuthService auth)
    {
        _db   = db;
        _auth = auth;
    }

    /// <summary>
    /// Actualiza el plan del usuario (free ↔ pro).
    /// Por ahora no requiere pago real — solo cambia el campo en DB.
    /// </summary>
    [HttpPut("plan")]
    public async Task<IActionResult> UpdatePlan([FromBody] UpdatePlanDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user   = await _db.Users.FindAsync(userId);

        if (user is null)
            return NotFound(new { message = "Usuario no encontrado." });

        var plan = dto.Plan.ToLower();
        if (plan != "free" && plan != "pro")
            return BadRequest(new { message = "El plan debe ser 'free' o 'pro'." });

        user.Plan = plan;
        await _db.SaveChangesAsync();

        // Devolver perfil actualizado
        var updatedUser = await _auth.GetUserByIdAsync(userId);
        return Ok(updatedUser);
    }
}
