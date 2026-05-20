using CargoExpreso.API.DTOs.Redemptions;
using FluentValidation;

namespace CargoExpreso.API.Validators;

public class CreateRedemptionValidator : AbstractValidator<CreateRedemptionRequest>
{
    public CreateRedemptionValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto de canje debe ser mayor a cero")
            .LessThanOrEqualTo(100_000).WithMessage("El monto supera el límite permitido por transacción");
    }
}
