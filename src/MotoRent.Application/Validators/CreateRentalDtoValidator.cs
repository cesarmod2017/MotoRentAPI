using FluentValidation;
using MotoRent.Application.DTOs.Rental;

namespace MotoRent.Application.Validators
{
    public class CreateRentalDtoValidator : AbstractValidator<CreateRentalDto>
    {
        public CreateRentalDtoValidator()
        {
            RuleFor(x => x.DeliverymanId)
                .NotEmpty().WithMessage("O ID do entregador é obrigatório.");

            RuleFor(x => x.MotorcycleId)
                .NotEmpty().WithMessage("O ID da motocicleta é obrigatório.");

            RuleFor(x => x.Plan)
                .NotEmpty().WithMessage("O plano é obrigatório.")
                .Must(x => x == 7 || x == 15 || x == 30 || x == 45 || x == 50)
                .WithMessage("O plano deve ser de 7, 15, 30, 45 ou 50 dias.");
        }
    }
}
