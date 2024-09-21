using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;
using MotoRent.Infrastructure.Exceptions;
using MotoRent.MessageConsumers.Events;
using MotoRent.MessageConsumers.Services;

namespace MotoRent.Application.Services
{
    public class MotorcycleService : IMotorcycleService
    {
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateMotorcycleDto> _createMotorcycleValidator;
        private readonly ILogger<MotorcycleService> _logger;
        private readonly IMessageService _messageService;
        private readonly IRentalRepository _rentalRepository;

        public MotorcycleService(
            IMotorcycleRepository motorcycleRepository,
            IMapper mapper,
            IValidator<CreateMotorcycleDto> createMotorcycleValidator,
            ILogger<MotorcycleService> logger,
            IMessageService messageService,
            IRentalRepository rentalRepository)
        {
            _motorcycleRepository = motorcycleRepository ?? throw new ArgumentNullException(nameof(motorcycleRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createMotorcycleValidator = createMotorcycleValidator ?? throw new ArgumentNullException(nameof(createMotorcycleValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageService = messageService;
            _rentalRepository = rentalRepository;
        }

        public async Task<IEnumerable<MotorcycleDto>> GetAllMotorcyclesAsync()
        {
            _logger.LogInformation("Buscando todas as motos");
            var motorcycles = await _motorcycleRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<MotorcycleDto>>(motorcycles);
        }

        public async Task<MotorcycleDto> GetMotorcycleByIdAsync(string id)
        {
            _logger.LogInformation("Buscando moto com ID: {MotorcycleId}", id);
            var motorcycle = await _motorcycleRepository.GetByFieldStringAsync("identifier", id);
            if (motorcycle == null)
            {
                _logger.LogWarning("Moto não encontrada com ID: {MotorcycleId}", id);
                throw new NotFoundException($"Moto não encontrada: {id}");
            }
            return _mapper.Map<MotorcycleDto>(motorcycle);
        }

        public async Task<MotorcycleDto> CreateMotorcycleAsync(CreateMotorcycleDto createMotorcycleDto)
        {
            _logger.LogInformation("Criando nova moto: {@CreateMotorcycleDto}", createMotorcycleDto);

            var validationResult = await _createMotorcycleValidator.ValidateAsync(createMotorcycleDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Falha na validação para a solicitação de criação de moto: {@ValidationErrors}", validationResult.Errors);
                throw new ValidationException(validationResult.Errors);
            }

            var existingMotorcycle = await _motorcycleRepository.GetByLicensePlateAsync(createMotorcycleDto.LicensePlate);
            if (existingMotorcycle != null && existingMotorcycle.Any())
            {
                throw new ArgumentException("Já existe uma moto cadastrada com esta placa. Por favor, verifique e tente novamente com uma placa diferente.");
            }

            var existingMotorcycleByIdentifier = await _motorcycleRepository.GetByFieldStringAsync("identifier", createMotorcycleDto.Identifier);
            if (existingMotorcycleByIdentifier != null)
            {
                throw new ArgumentException("Já existe uma moto cadastrada com este identificador. Por favor, use um identificador único.");
            }

            var motorcycle = _mapper.Map<MotorcycleModel>(createMotorcycleDto);
            await _motorcycleRepository.CreateAsync(motorcycle);

            _logger.LogInformation("Moto criada com sucesso: {@Motorcycle}", motorcycle);

            await PublishMotorcycleCreatedEventAsync(motorcycle);

            return _mapper.Map<MotorcycleDto>(motorcycle);
        }

        private async Task PublishMotorcycleCreatedEventAsync(MotorcycleModel motorcycle)
        {
            var motorcycleCreatedEvent = new MotorcycleCreatedEvent
            {
                Id = motorcycle.Id,
                Identifier = motorcycle.Identifier,
                Year = motorcycle.Year,
                Model = motorcycle.Model,
                LicensePlate = motorcycle.LicensePlate
            };
            await _messageService.PublishAsync("motorcycle-created", motorcycleCreatedEvent);
        }

        public async Task UpdateMotorcycleLicensePlateAsync(string id, UpdateLicensePlateDto updateLicensePlateDto)
        {
            _logger.LogInformation("Atualizando placa da moto com ID: {MotorcycleId}", id);
            var motorcycle = await _motorcycleRepository.GetByFieldStringAsync("identifier", id);
            if (motorcycle == null)
            {
                _logger.LogWarning("Moto não encontrada com ID: {MotorcycleId}", id);
                throw new ArgumentException("Moto não encontrada", nameof(id));
            }

            motorcycle.LicensePlate = updateLicensePlateDto.LicensePlate;
            await _motorcycleRepository.UpdateAsync(motorcycle.Id, motorcycle);
            _logger.LogInformation("Placa atualizada com sucesso para a moto com ID: {MotorcycleId}", id);
        }

        public async Task DeleteMotorcycleAsync(string id)
        {
            var motorcycle = await _motorcycleRepository.GetByFieldStringAsync("identifier", id);
            if (motorcycle == null)
            {
                _logger.LogWarning("Moto não encontrada com ID: {MotorcycleId}", id);
                throw new NotFoundException($"Moto não encontrada: {id}");
            }

            var rentalExists = await _rentalRepository.ExistsForMotorcycleAsync(id);
            if (rentalExists)
            {
                throw new InvalidOperationException("Não é possível remover a moto pois existem locações associadas a ela.");
            }

            _logger.LogInformation("Excluindo moto com ID: {MotorcycleId}", id);

            await _motorcycleRepository.DeleteAsync(motorcycle.Id);

            _logger.LogInformation("Moto excluída com sucesso com ID: {MotorcycleId}", id);
        }

        public async Task<IEnumerable<MotorcycleDto>> GetMotorcyclesByLicensePlateAsync(string licensePlate)
        {
            _logger.LogInformation("Buscando motos com placa: {LicensePlate}", licensePlate);
            var motorcycles = await _motorcycleRepository.GetByLicensePlateAsync(licensePlate);
            return _mapper.Map<IEnumerable<MotorcycleDto>>(motorcycles);
        }

        public async Task<string> GetRandomMotorcycleIdAsync()
        {
            var motorcycles = await _motorcycleRepository.GetAllAsync();
            var randomMotorcycle = motorcycles.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
            return randomMotorcycle?.Identifier;
        }
    }
}
