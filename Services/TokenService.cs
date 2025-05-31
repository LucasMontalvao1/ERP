using API.Configuration;
using API.Constants;
using API.Models.DTOs.Auth;
using API.Services.Cache.Interfaces;
using API.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace API.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ICacheService _cacheService;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        ICacheService cacheService,
        ILogger<TokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _cacheService = cacheService;
        _logger = logger;
    }

    public string GenerateAccessToken(UsuarioInfo usuario)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ApiConstants.Auth.ClaimTypes.Login, usuario.Login),
            new(ClaimTypes.Email, usuario.Email),
            new(ApiConstants.Auth.ClaimTypes.FirstAccess, usuario.PrimeiroAcesso.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        foreach (var role in usuario.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _jwtSettings.ValidateIssuer,
                ValidateAudience = _jwtSettings.ValidateAudience,
                ValidateLifetime = _jwtSettings.ValidateLifetime,
                ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Token inválido: {Error}", ex.Message);
            return null;
        }
    }

    public async Task<bool> IsTokenValidAsync(string token)
    {
        try
        {
            var principal = ValidateToken(token);
            if (principal == null) return false;

            // Verificar se token está na blacklist
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti))
            {
                var blacklistKey = $"{ApiConstants.CachePrefixes.Authentication}blacklist:{jti}";
                var isBlacklisted = await _cacheService.GetAsync(blacklistKey);
                if (!string.IsNullOrEmpty(isBlacklisted))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task InvalidateTokenAsync(string token)
    {
        try
        {
            var principal = ValidateToken(token);
            var jti = principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!string.IsNullOrEmpty(jti))
            {
                var blacklistKey = $"{ApiConstants.CachePrefixes.Authentication}blacklist:{jti}";
                await _cacheService.SetAsync(
                    blacklistKey,
                    "blacklisted",
                    TimeSpan.FromMinutes(_jwtSettings.ExpirationMinutes)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao invalidar token");
        }
    }

    public async Task InvalidateAllUserTokensAsync(int usuarioId)
    {
        try
        {
            var versionKey = $"{ApiConstants.CachePrefixes.Authentication}version:{usuarioId}";
            await _cacheService.IncrementAsync(versionKey);

            _logger.LogInformation("Todos os tokens do usuário {UserId} foram invalidados", usuarioId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao invalidar tokens do usuário {UserId}", usuarioId);
        }
    }

    public async Task<UsuarioInfo?> GetUserFromTokenAsync(string token)
    {
        try
        {
            var principal = ValidateToken(token);
            if (principal == null) return null;

            return new UsuarioInfo
            {
                Id = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
                Nome = principal.FindFirst(ClaimTypes.Name)?.Value ?? "",
                Login = principal.FindFirst(ApiConstants.Auth.ClaimTypes.Login)?.Value ?? "",
                Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? "",
                Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                PrimeiroAcesso = bool.Parse(principal.FindFirst(ApiConstants.Auth.ClaimTypes.FirstAccess)?.Value ?? "false")
            };
        }
        catch
        {
            return null;
        }
    }
}