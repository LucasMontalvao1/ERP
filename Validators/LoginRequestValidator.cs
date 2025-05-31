using API.Models.DTOs.Auth;
using API.Constants;
using FluentValidation;

namespace API.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Login é obrigatório")
            .Length(ApiConstants.Validation.MinUsernameLength, ApiConstants.Validation.MaxUsernameLength)
            .WithMessage($"Login deve ter entre {ApiConstants.Validation.MinUsernameLength} e {ApiConstants.Validation.MaxUsernameLength} caracteres");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .Length(ApiConstants.Validation.MinPasswordLength, ApiConstants.Validation.MaxPasswordLength)
            .WithMessage($"Senha deve ter entre {ApiConstants.Validation.MinPasswordLength} e {ApiConstants.Validation.MaxPasswordLength} caracteres");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.SenhaAtual)
            .NotEmpty().WithMessage("Senha atual é obrigatória");

        RuleFor(x => x.NovaSenha)
            .NotEmpty().WithMessage("Nova senha é obrigatória")
            .Length(ApiConstants.Validation.MinPasswordLength, ApiConstants.Validation.MaxPasswordLength)
            .WithMessage($"Nova senha deve ter entre {ApiConstants.Validation.MinPasswordLength} e {ApiConstants.Validation.MaxPasswordLength} caracteres")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("Nova senha deve conter pelo menos: 1 letra minúscula, 1 maiúscula, 1 número e 1 caractere especial");

        RuleFor(x => x.ConfirmarSenha)
            .NotEmpty().WithMessage("Confirmação de senha é obrigatória")
            .Equal(x => x.NovaSenha).WithMessage("Confirmação de senha não confere");
    }
}