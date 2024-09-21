using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MotoRent.Application.DTOs.Rental;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Application.Services
{
    public class RentalService : IRentalService
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly IDeliverymanRepository _deliverymanRepository;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateRentalDto> _createRentalValidator;
        private readonly ILogger<RentalService> _logger;

        public RentalService(
            IRentalRepository rentalRepository,
            IDeliverymanRepository deliverymanRepository,
            IMotorcycleRepository motorcycleRepository,
            IMapper mapper,
            IValidator<CreateRentalDto> createRentalValidator,
            ILogger<RentalService> logger)
        {
            _rentalRepository = rentalRepository ?? throw new ArgumentNullException(nameof(rentalRepository));
            _deliverymanRepository = deliverymanRepository ?? throw new ArgumentNullException(nameof(deliverymanRepository));
            _motorcycleRepository = motorcycleRepository ?? throw new ArgumentNullException(nameof(motorcycleRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createRentalValidator = createRentalValidator ?? throw new ArgumentNullException(nameof(createRentalValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RentalDto> CreateRentalAsync(CreateRentalDto createRentalDto)
        {
            _logger.LogInformation("Criando nova locação: {@CreateRentalDto}", createRentalDto);

            var validationResult = await _createRentalValidator.ValidateAsync(createRentalDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Falha na validação para a solicitação de criação de locação: {@ValidationErrors}", validationResult.Errors);
                throw new ValidationException(validationResult.Errors);
            }

            var rentalExist = await _rentalRepository.GetByIdentifierAsync(createRentalDto.Identifier);
            if (rentalExist != null)
            {
                _logger.LogWarning("Este identificador já existe para outra locação: {Identifier}", createRentalDto.Identifier);
                throw new ArgumentException($"Este identificador já existe para outra locação: {createRentalDto.Identifier}");
            }

            var deliveryman = await _deliverymanRepository.GetByIdentifierAsync(createRentalDto.DeliverymanId);
            if (deliveryman == null)
            {
                _logger.LogWarning("Entregador não encontrado com o ID: {DeliverymanId}", createRentalDto.DeliverymanId);
                throw new ArgumentException("Entregador não encontrado");
            }

            if (deliveryman.LicenseType != "A" && deliveryman.LicenseType != "AB")
            {
                _logger.LogWarning("O entregador {DeliverymanId} não possui o tipo de licença necessário", createRentalDto.DeliverymanId);
                throw new ArgumentException("O entregador deve ter licença do tipo A ou AB");
            }

            var motorcycle = await _motorcycleRepository.GetByIdentifierAsync(createRentalDto.MotorcycleId);
            if (motorcycle == null)
            {
                _logger.LogWarning("Motocicleta não encontrada com o ID: {MotorcycleId}", createRentalDto.MotorcycleId);
                throw new ArgumentException("Motocicleta não encontrada");
            }

            var rental = _mapper.Map<RentalModel>(createRentalDto);
            rental.DailyRate = CalculateDailyRate(createRentalDto.Plan);
            rental.StartDate = DateTime.UtcNow.Date.AddDays(1);
            rental.EndDate = rental.StartDate.AddDays(createRentalDto.Plan);
            rental.ExpectedEndDate = rental.EndDate;

            await _rentalRepository.CreateAsync(rental);

            _logger.LogInformation("Locação criada com sucesso: {@Rental}", rental);

            return _mapper.Map<RentalDto>(rental);
        }

        public async Task<RentalDto> GetRentalByIdAsync(string id)
        {
            _logger.LogInformation("Buscando locação com ID: {RentalId}", id);
            var rental = await _rentalRepository.GetByIdentifierAsync(id);
            if (rental == null)
            {
                _logger.LogWarning("Locação não encontrada com ID: {RentalId}", id);
                throw new ArgumentException("Locação não encontrada", nameof(id));
            }
            return _mapper.Map<RentalDto>(rental);
        }

        public async Task<RentalCalculationResultDto> CalculateRentalCostAsync(string id, UpdateReturnDateDto returnDateDto)
        {
            _logger.LogInformation("Calculando custo da locação para o ID: {RentalId} com data de retorno: {ReturnDate}", id, returnDateDto.ReturnDate);

            var rental = await _rentalRepository.GetByIdentifierAsync(id);
            if (rental == null)
            {
                _logger.LogWarning("Locação não encontrada com ID: {RentalId}", id);
                throw new ArgumentException("Locação não encontrada", nameof(id));
            }

            var totalDays = (int)(returnDateDto.ReturnDate - rental.StartDate).TotalDays;
            var plannedDays = (int)(rental.ExpectedEndDate - rental.StartDate).TotalDays;

            decimal totalCost = 0;
            string message = "";

            if (totalDays <= plannedDays)
            {
                totalCost = totalDays * rental.DailyRate;
                if (totalDays < plannedDays)
                {
                    var unusedDays = plannedDays - totalDays;
                    var penaltyRate = GetPenaltyRate(rental.Plan);
                    var penaltyCost = unusedDays * rental.DailyRate * penaltyRate;
                    var rentalCost = totalDays * rental.DailyRate;
                    totalCost = rentalCost + penaltyCost;
                    message = $"Devolução antecipada. Valor da diária: {rentalCost:C}. Multa aplicada: {penaltyCost:C}. Valor total a ser pago: {totalCost:C}";
                    _logger.LogInformation("Devolução antecipada para locação {RentalId}. Multa aplicada: {PenaltyCost}", id, penaltyCost);
                }
                else
                {
                    message = $"Devolução no prazo. Valor total a ser pago: {totalCost:C}";
                }
            }
            else
            {
                var extraDays = totalDays - plannedDays;
                var regularCost = plannedDays * rental.DailyRate;
                var extraCost = extraDays * 50;
                totalCost = regularCost + extraCost;
                message = $"Devolução atrasada. Valor da diária: {regularCost:C}. Cobrança extra por {extraDays} dias: {extraCost:C}. Valor total a ser pago: {totalCost:C}";
                _logger.LogInformation("Devolução atrasada para locação {RentalId}. Cobrança extra: {ExtraCharge}", id, extraCost);
            }

            await _rentalRepository.UpdateReturnDateAsync(id, returnDateDto.ReturnDate, totalCost);

            _logger.LogInformation("Custo da locação calculado para locação {RentalId}. Custo total: {TotalCost}", id, totalCost);

            return new RentalCalculationResultDto
            {
                TotalCost = totalCost,
                Message = message
            };
        }

        private decimal CalculateDailyRate(int plan)
        {
            return plan switch
            {
                7 => 30.0m,
                15 => 28.0m,
                30 => 22.0m,
                45 => 20.0m,
                50 => 18.0m,
                _ => throw new ArgumentException("Plano inválido")
            };
        }

        private decimal GetPenaltyRate(int plan)
        {
            return plan switch
            {
                7 => 0.20m,
                15 => 0.40m,
                _ => 0
            };
        }
    }
}
