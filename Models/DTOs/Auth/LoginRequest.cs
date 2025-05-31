using API.Constants;
using System.ComponentModel.DataAnnotations;

namespace API.Models.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Login é obrigatório")]
    [StringLength(ApiConstants.Validation.MaxUsernameLength,
        MinimumLength = ApiConstants.Validation.MinUsernameLength,
        ErrorMessage = "Login deve ter entre {2} e {1} caracteres")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    [StringLength(ApiConstants.Validation.MaxPasswordLength,
        MinimumLength = ApiConstants.Validation.MinPasswordLength,
        ErrorMessage = "Senha deve ter entre {2} e {1} caracteres")]
    public string Senha { get; set; } = string.Empty;

    public bool LembrarMe { get; set; } = false;
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token é obrigatório")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Senha atual é obrigatória")]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [StringLength(ApiConstants.Validation.MaxPasswordLength,
        MinimumLength = ApiConstants.Validation.MinPasswordLength,
        ErrorMessage = "Nova senha deve ter entre {2} e {1} caracteres")]
    public string NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
    [Compare(nameof(NovaSenha), ErrorMessage = "Confirmação de senha não confere")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}