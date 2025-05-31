using API.Models.DTOs.Auth;
using API.Models.Responses;

namespace API.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string ipAddress, string userAgent);
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<ApiResponse<bool>> LogoutAsync(string refreshToken);
    Task<ApiResponse<TokenValidationResponse>> ValidateTokenAsync(string token);
    Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<bool> IsUserLockedAsync(string login);
    Task LogLoginAttemptAsync(string login, bool success, string ipAddress, string userAgent);
}