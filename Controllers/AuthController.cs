using API.Constants;
using API.Models.DTOs.Auth;
using API.Models.Responses;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza login do usuário
    /// </summary>
    /// <param name="request">Dados de login</param>
    /// <returns>Token JWT e informações do usuário</returns>
    [HttpPost("login")]
    [EnableRateLimiting(ApiConstants.RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<object>.ErrorResult(string.Join("; ", errors)));
        }

        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var result = await _authService.LoginAsync(request, ipAddress, userAgent);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Renova o token de acesso usando refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>Novo token JWT</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ApiConstants.ErrorMessages.ValidationError));
        }

        var ipAddress = GetClientIpAddress();
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Realiza logout do usuário
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>Confirmação de logout</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request)
    {
        if (request != null && !string.IsNullOrEmpty(request.RefreshToken))
        {
            await _authService.LogoutAsync(request.RefreshToken);
        }

        return Ok(ApiResponse<bool>.SuccessResult(true, ApiConstants.Auth.Messages.LogoutSuccess));
    }

    /// <summary>
    /// Valida se o token atual é válido
    /// </summary>
    /// <returns>Status de validação do token</returns>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TokenValidationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateToken()
    {
        var token = Request.Headers["Authorization"]
            .FirstOrDefault()?.Split(" ").Last();

        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(ApiResponse<object>.ErrorResult("Token não fornecido"));
        }

        var result = await _authService.ValidateTokenAsync(token);

        if (!result.Success || !result.Data?.Valido == true)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Obtém informações do usuário logado
    /// </summary>
    /// <returns>Informações do usuário</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UsuarioInfo>), StatusCodes.Status200OK)]
    public IActionResult GetCurrentUser()
    {
        var usuarioInfo = new UsuarioInfo
        {
            Id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
            Nome = User.FindFirst(ClaimTypes.Name)?.Value ?? "",
            Login = User.FindFirst(ApiConstants.Auth.ClaimTypes.Login)?.Value ?? "",
            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "",
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
            PrimeiroAcesso = bool.Parse(User.FindFirst(ApiConstants.Auth.ClaimTypes.FirstAccess)?.Value ?? "false")
        };

        return Ok(ApiResponse<UsuarioInfo>.SuccessResult(usuarioInfo));
    }

    /// <summary>
    /// Altera a senha do usuário logado
    /// </summary>
    /// <param name="request">Dados para alteração da senha</param>
    /// <returns>Confirmação da alteração</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<object>.ErrorResult(string.Join("; ", errors)));
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var result = await _authService.ChangePasswordAsync(userId, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Verifica o status de saúde do sistema de autenticação
    /// </summary>
    /// <returns>Status do sistema</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        var healthInfo = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = ApiConstants.ApiVersion,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        };

        return Ok(ApiResponse<object>.SuccessResult(healthInfo, "Sistema de autenticação funcionando"));
    }

    private string GetClientIpAddress()
    {
        // Verificar se há proxy reverso
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}