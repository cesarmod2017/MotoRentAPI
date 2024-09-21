using FluentValidation;
using MotoRent.Application.DTOs.Motorcycle;

namespace MotoRent.Application.Validators
{
    public partial class CreateMotorcycleDtoValidator : AbstractValidator<CreateMotorcycleDto>
    {
        public CreateMotorcycleDtoValidator()
        {
            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("O identificador é obrigatório.")
                .MaximumLength(50).WithMessage("O identificador não deve exceder 50 caracteres.");

            RuleFor(x => x.Year)
                .InclusiveBetween(1900, System.DateTime.Now.Year + 1)
                .WithMessage("O ano deve estar entre 1900 e o próximo ano.");

            RuleFor(x => x.Model)
                .NotEmpty().WithMessage("O modelo é obrigatório.")
                .MaximumLength(100).WithMessage("O modelo não deve exceder 100 caracteres.");

            RuleFor(x => x.LicensePlate)
                .NotEmpty().WithMessage("A placa é obrigatória.")
                .Matches(@"^[A-Z]{3}-\d{4}$").WithMessage("A placa deve estar no formato ABC-1234.");
        }
    }
}
