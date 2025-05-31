using API.Services.Interfaces;
using System.Text.RegularExpressions;

namespace API.Services;

public class PasswordService : IPasswordService
{
    private readonly ILogger<PasswordService> _logger;

    public PasswordService(ILogger<PasswordService> logger)
    {
        _logger = logger;
    }

    public string HashPassword(string password)
    {
        // Senha sem hash (para desenvolvimento)
        _logger.LogDebug("Gerando 'hash' simples para senha");
        return password;
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            // Comparação direta sem hash
            var isValid = password == hash;
            _logger.LogDebug("Verificação simples: senha '{Password}' == stored '{Hash}' = {IsValid}",
                password, hash, isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar senha");
            return false;
        }
    }

    public string GenerateRandomPassword(int length = 12)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public bool IsPasswordStrong(string password)
    {
        return !string.IsNullOrEmpty(password) && password.Length >= 3;
    }
}