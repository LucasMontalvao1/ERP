using API.Models.DTOs.Auth;
using System.Security.Claims;

namespace API.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(UsuarioInfo usuario);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    Task<bool> IsTokenValidAsync(string token);
    Task InvalidateTokenAsync(string token);
    Task InvalidateAllUserTokensAsync(int usuarioId);
    Task<UsuarioInfo?> GetUserFromTokenAsync(string token);
}