using CargoExpreso.API.DTOs.Shipments;
using FluentValidation;

namespace CargoExpreso.API.Validators;

public class ScanShipmentValidator : AbstractValidator<ScanShipmentRequest>
{
    public ScanShipmentValidator()
    {
        RuleFor(x => x.ShipmentNumber)
            .NotEmpty().WithMessage("El número de guía es requerido")
            .MaximumLength(50).WithMessage("El número de guía no puede superar 50 caracteres");
    }
}
