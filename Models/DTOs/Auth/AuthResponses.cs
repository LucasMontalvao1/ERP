namespace API.Models.DTOs.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UsuarioInfo Usuario { get; set; } = new();
}

public class UsuarioInfo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime? UltimoLogin { get; set; }
    public bool PrimeiroAcesso { get; set; }
}

public class TokenValidationResponse
{
    public bool Valido { get; set; }
    public string? Motivo { get; set; }
    public DateTime? Expira { get; set; }
    public UsuarioInfo? Usuario { get; set; }
}