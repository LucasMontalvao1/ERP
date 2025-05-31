using API.Models.Entities;
using API.Repositories.Base.Interfaces;

namespace API.Repositories.Interfaces;

public interface IUsuarioRepository : IBaseRepository<Usuario>
{
    Task<Usuario?> GetByLoginAsync(string login);
    Task<Usuario?> GetByEmailAsync(string email);
    Task<List<string>> GetUserRolesAsync(int usuarioId);
    Task UpdateLastLoginAsync(int usuarioId);
    Task<int> IncrementLoginAttemptsAsync(int usuarioId);
    Task ResetLoginAttemptsAsync(int usuarioId);
    Task LogLoginAttemptAsync(string login, bool success, string ipAddress, string userAgent);
    Task UpdatePasswordAsync(int usuarioId, string newPasswordHash);
    Task UpdateFirstAccessAsync(int usuarioId, bool firstAccess);
    Task<List<Usuario>> GetActiveUsersAsync();
    Task<bool> LoginExistsAsync(string login, int? excludeUserId = null);
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
}