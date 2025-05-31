using API.Configuration;
using API.Constants;
using API.Models.DTOs.Auth;
using API.Models.Responses;
using API.Repositories.Interfaces;
using API.Services.Cache.Interfaces;
using API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace API.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        ITokenService tokenService,
        IPasswordService passwordService,
        ICacheService cacheService,
        ILogger<AuthService> logger,
        IOptions<JwtSettings> jwtSettings)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _passwordService = passwordService;
        _cacheService = cacheService;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string ipAddress, string userAgent)
    {
        try
        {
            // Verificar rate limiting
            if (await IsUserLockedAsync(request.Login))
            {
                await LogLoginAttemptAsync(request.Login, false, ipAddress, userAgent);
                return ApiResponse<LoginResponse>.ErrorResult(ApiConstants.Auth.Messages.UserLocked);
            }

            // Buscar usuário
            var usuario = await _usuarioRepository.GetByLoginAsync(request.Login);
            if (usuario == null)
            {
                await LogLoginAttemptAsync(request.Login, false, ipAddress, userAgent);
                await IncrementLoginAttemptsAsync(request.Login);
                return ApiResponse<LoginResponse>.ErrorResult(ApiConstants.Auth.Messages.LoginFailed);
            }

            // Verificar senha
            if (!_passwordService.VerifyPassword(request.Senha, usuario.SenhaHash))
            {
                await LogLoginAttemptAsync(request.Login, false, ipAddress, userAgent);
                await IncrementLoginAttemptsAsync(request.Login);
                await _usuarioRepository.IncrementLoginAttemptsAsync(usuario.Id);
                return ApiResponse<LoginResponse>.ErrorResult(ApiConstants.Auth.Messages.LoginFailed);
            }

            // Verificar se usuário está ativo
            if (!usuario.Ativo)
            {
                await LogLoginAttemptAsync(request.Login, false, ipAddress, userAgent);
                return ApiResponse<LoginResponse>.ErrorResult(ApiConstants.Auth.Messages.UserInactive);
            }

            // Buscar roles
            var roles = await _usuarioRepository.GetUserRolesAsync(usuario.Id);

            // Criar info do usuário
            var usuarioInfo = new UsuarioInfo
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Login = usuario.Login,
                Email = usuario.Email,
                Roles = roles,
                UltimoLogin = usuario.UltimoLogin,
                PrimeiroAcesso = usuario.PrimeiroAcesso
            };

            // Gerar tokens
            var accessToken = _tokenService.GenerateAccessToken(usuarioInfo);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Salvar refresh token no cache
            var expiration = TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);
            await _cacheService.SetAsync(
                ApiConstants.Auth.CacheKeys.RefreshToken(refreshToken),
                usuario.Id.ToString(),
                expiration
            );

            // Atualizar último login e resetar tentativas
            await _usuarioRepository.UpdateLastLoginAsync(usuario.Id);
            await _usuarioRepository.ResetLoginAttemptsAsync(usuario.Id);
            await ResetLoginAttemptsAsync(request.Login);

            // Log sucesso
            await LogLoginAttemptAsync(request.Login, true, ipAddress, userAgent);

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                Usuario = usuarioInfo
            };

            var message = usuario.PrimeiroAcesso
                ? ApiConstants.Auth.Messages.FirstAccess
                : ApiConstants.Auth.Messages.LoginSuccess;

            _logger.LogInformation("Login realizado com sucesso para usuário {Login} de {IpAddress}",
                request.Login, ipAddress);

            return ApiResponse<LoginResponse>.SuccessResult(response, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar login para usuário {Login}", request.Login);
            return ApiResponse<LoginResponse>.ErrorResult(ApiConstants.ErrorMessages.InternalError);
        }
    }

    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        try
        {
            // Verificar se refresh token existe no cache
            var userIdString = await _cacheService.GetAsync(ApiConstants.Auth.CacheKeys.RefreshToken(refreshToken));
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return ApiResponse<LoginResponse>.ErrorResult(ApiConstants.Auth.Messages.TokenInvalid);
            }

            // Buscar usuário
            var usuario = await _usuarioRepository.GetByIdAsync(userId);
            if (usuario == null || !usuario.Ativo)
            {
                return ApiResponse<LoginResponse>.ErrorResult(ApiConstants.ErrorMessages.NotFound);
            }

            // Buscar roles
            var roles = await _usuarioRepository.GetUserRolesAsync(usuario.Id);

            var usuarioInfo = new UsuarioInfo
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Login = usuario.Login,
                Email = usuario.Email,
                Roles = roles,
                UltimoLogin = usuario.UltimoLogin,
                PrimeiroAcesso = usuario.PrimeiroAcesso
            };

            // Gerar novos tokens
            var newAccessToken = _tokenService.GenerateAccessToken(usuarioInfo);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Remover token antigo e adicionar novo
            await _cacheService.RemoveAsync(ApiConstants.Auth.CacheKeys.RefreshToken(refreshToken));
            await _cacheService.SetAsync(
                ApiConstants.Auth.CacheKeys.RefreshToken(newRefreshToken),
                usuario.Id.ToString(),
                TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays)
            );

            var response = new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                Usuario = usuarioInfo
            };

            _logger.LogInformation("Token renovado com sucesso para usuário {UserId} de {IpAddress}",
                userId, ipAddress);

            return ApiResponse<LoginResponse>.SuccessResult(response, ApiConstants.Auth.Messages.TokenRefreshed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao renovar token");
            return ApiResponse<LoginResponse>.ErrorResult(ApiConstants.ErrorMessages.InternalError);
        }
    }

    public async Task<ApiResponse<bool>> LogoutAsync(string refreshToken)
    {
        try
        {
            await _cacheService.RemoveAsync(ApiConstants.Auth.CacheKeys.RefreshToken(refreshToken));
            _logger.LogInformation("Logout realizado com sucesso");
            return ApiResponse<bool>.SuccessResult(true, ApiConstants.Auth.Messages.LogoutSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar logout");
            return ApiResponse<bool>.ErrorResult(ApiConstants.ErrorMessages.InternalError);
        }
    }

    public async Task<ApiResponse<TokenValidationResponse>> ValidateTokenAsync(string token)
    {
        try
        {
            var isValid = await _tokenService.IsTokenValidAsync(token);

            if (!isValid)
            {
                return ApiResponse<TokenValidationResponse>.SuccessResult(
                    new TokenValidationResponse { Valido = false, Motivo = "Token inválido ou expirado" });
            }

            var usuario = await _tokenService.GetUserFromTokenAsync(token);

            return ApiResponse<TokenValidationResponse>.SuccessResult(
                new TokenValidationResponse
                {
                    Valido = true,
                    Usuario = usuario,
                    Expira = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
                },
                ApiConstants.Auth.Messages.TokenValidated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar token");
            return ApiResponse<TokenValidationResponse>.ErrorResult(ApiConstants.ErrorMessages.InternalError);
        }
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        try
        {
            var usuario = await _usuarioRepository.GetByIdAsync(userId);
            if (usuario == null)
            {
                return ApiResponse<bool>.ErrorResult(ApiConstants.ErrorMessages.NotFound);
            }

            // Verificar senha atual (exceto no primeiro acesso)
            if (!usuario.PrimeiroAcesso && !_passwordService.VerifyPassword(request.SenhaAtual, usuario.SenhaHash))
            {
                return ApiResponse<bool>.ErrorResult("Senha atual incorreta");
            }

            // Validar força da nova senha
            if (!_passwordService.IsPasswordStrong(request.NovaSenha))
            {
                return ApiResponse<bool>.ErrorResult("Nova senha não atende aos critérios de segurança");
            }

            // Atualizar senha
            var novoHash = _passwordService.HashPassword(request.NovaSenha);
            await _usuarioRepository.UpdatePasswordAsync(userId, novoHash);

            // Se é primeiro acesso, marcar como não sendo mais
            if (usuario.PrimeiroAcesso)
            {
                await _usuarioRepository.UpdateFirstAccessAsync(userId, false);
            }

            // Invalidar todos os tokens do usuário
            await _tokenService.InvalidateAllUserTokensAsync(userId);

            _logger.LogInformation("Senha alterada com sucesso para usuário {UserId}", userId);

            return ApiResponse<bool>.SuccessResult(true, ApiConstants.Auth.Messages.PasswordChanged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar senha do usuário {UserId}", userId);
            return ApiResponse<bool>.ErrorResult(ApiConstants.ErrorMessages.InternalError);
        }
    }

    public async Task<bool> IsUserLockedAsync(string login)
    {
        try
        {
            var lockKey = ApiConstants.Auth.CacheKeys.UserLock(login);
            var isLocked = await _cacheService.GetAsync(lockKey);
            return !string.IsNullOrEmpty(isLocked);
        }
        catch
        {
            return false;
        }
    }

    public async Task LogLoginAttemptAsync(string login, bool success, string ipAddress, string userAgent)
    {
        try
        {
            await _usuarioRepository.LogLoginAttemptAsync(login, success, ipAddress, userAgent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar tentativa de login");
        }
    }

    private async Task IncrementLoginAttemptsAsync(string login)
    {
        try
        {
            var attemptsKey = ApiConstants.Auth.CacheKeys.LoginAttempts(login);
            var attempts = await _cacheService.IncrementAsync(attemptsKey);

            if (attempts >= ApiConstants.Defaults.MaxLoginAttempts)
            {
                var lockKey = ApiConstants.Auth.CacheKeys.UserLock(login);
                await _cacheService.SetAsync(lockKey, "locked", TimeSpan.FromMinutes(ApiConstants.Defaults.LockoutMinutes));

                _logger.LogWarning("Usuário {Login} bloqueado por múltiplas tentativas inválidas", login);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao incrementar tentativas de login para {Login}", login);
        }
    }

    private async Task ResetLoginAttemptsAsync(string login)
    {
        try
        {
            await _cacheService.RemoveAsync(ApiConstants.Auth.CacheKeys.LoginAttempts(login));
            await _cacheService.RemoveAsync(ApiConstants.Auth.CacheKeys.UserLock(login));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao resetar tentativas de login para {Login}", login);
        }
    }
}