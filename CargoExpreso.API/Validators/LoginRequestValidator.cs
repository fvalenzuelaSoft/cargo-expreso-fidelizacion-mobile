using CargoExpreso.API.DTOs.Auth;
using FluentValidation;

namespace CargoExpreso.API.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.IdentityNumber)
            .NotEmpty().WithMessage("El número de identidad es requerido")
            .MaximumLength(20).WithMessage("El número de identidad no puede superar 20 caracteres");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("El teléfono es requerido")
            .MaximumLength(20).WithMessage("El teléfono no puede superar 20 caracteres");
    }
}
