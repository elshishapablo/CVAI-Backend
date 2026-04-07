using CVMatchAI.API.DTOs;
using CVMatchAI.API.Models;

namespace CVMatchAI.API.Services;

public interface IAuthService
{
    /// <summary>Registra un nuevo usuario y devuelve token + datos</summary>
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);

    /// <summary>Autentica un usuario y devuelve token + datos</summary>
    Task<AuthResponseDto> LoginAsync(LoginDto dto);

    /// <summary>Obtiene los datos públicos del usuario por su Id</summary>
    Task<UserDto> GetUserByIdAsync(int userId);

    /// <summary>Genera un JWT para el usuario dado</summary>
    string GenerateJwt(User user);
}
