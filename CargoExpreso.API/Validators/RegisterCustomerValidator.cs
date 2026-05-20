using CargoExpreso.API.DTOs.Customers;
using FluentValidation;

namespace CargoExpreso.API.Validators;

public class RegisterCustomerValidator : AbstractValidator<RegisterCustomerRequest>
{
    public RegisterCustomerValidator()
    {
        RuleFor(x => x.IdentityNumber)
            .NotEmpty().WithMessage("El número de identidad es requerido")
            .MaximumLength(20);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("El teléfono es requerido")
            .MaximumLength(20);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El correo electrónico no es válido")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.BirthDate)
            .LessThan(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("La fecha de nacimiento debe ser anterior a hoy")
            .When(x => x.BirthDate.HasValue);
    }
}
