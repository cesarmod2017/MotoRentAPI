using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using MotoRent.Application.DTOs.Rental;
using MotoRent.Application.Services;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.UnitTests.Services;

public class RentalServiceTests
{
    private readonly Mock<IRentalRepository> _mockRentalRepository;
    private readonly Mock<IDeliverymanRepository> _mockDeliverymanRepository;
    private readonly Mock<IMotorcycleRepository> _mockMotorcycleRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IValidator<CreateRentalDto>> _mockCreateRentalValidator;
    private readonly Mock<ILogger<RentalService>> _mockLogger;
    private readonly RentalService _service;

    public RentalServiceTests()
    {
        _mockRentalRepository = new Mock<IRentalRepository>();
        _mockDeliverymanRepository = new Mock<IDeliverymanRepository>();
        _mockMotorcycleRepository = new Mock<IMotorcycleRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockCreateRentalValidator = new Mock<IValidator<CreateRentalDto>>();
        _mockLogger = new Mock<ILogger<RentalService>>();

        _service = new RentalService(
            _mockRentalRepository.Object,
            _mockDeliverymanRepository.Object,
            _mockMotorcycleRepository.Object,
            _mockMapper.Object,
            _mockCreateRentalValidator.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateRentalAsync_ValidDto_ReturnsCreatedRental()
    {

        var createDto = new CreateRentalDto
        {
            Identifier = "RENT123",
            DeliverymanId = "DEL123",
            MotorcycleId = "MOTO123",
            Plan = 7
        };

        var deliverymanModel = new DeliverymanModel
        {
            Identifier = "DEL123",
            LicenseType = "A"
        };

        var motorcycleModel = new MotorcycleModel
        {
            Identifier = "MOTO123"
        };

        var rentalModel = new RentalModel
        {
            Identifier = "RENT123",
            DeliverymanId = "DEL123",
            MotorcycleId = "MOTO123",
            DailyRate = 30.0m,
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(8),
            ExpectedEndDate = DateTime.UtcNow.Date.AddDays(8),
            Plan = 7
        };

        var rentalDto = new RentalDto
        {
            Identifier = "RENT123",
            DeliverymanId = "DEL123",
            MotorcycleId = "MOTO123",
            DailyRate = 30.0m,
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(8),
            ExpectedEndDate = DateTime.UtcNow.Date.AddDays(8),
            Plan = 7
        };

        _mockCreateRentalValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateRentalDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _mockDeliverymanRepository.Setup(r => r.GetByIdentifierAsync(createDto.DeliverymanId))
            .ReturnsAsync(deliverymanModel);

        _mockMotorcycleRepository.Setup(r => r.GetByIdentifierAsync(createDto.MotorcycleId))
            .ReturnsAsync(motorcycleModel);

        _mockMapper.Setup(m => m.Map<RentalModel>(createDto)).Returns(rentalModel);
        _mockMapper.Setup(m => m.Map<RentalDto>(rentalModel)).Returns(rentalDto);

        _mockRentalRepository.Setup(r => r.CreateAsync(It.IsAny<RentalModel>()))
            .ReturnsAsync(rentalModel);


        var result = await _service.CreateRentalAsync(createDto);


        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(rentalDto);
        _mockRentalRepository.Verify(r => r.CreateAsync(It.IsAny<RentalModel>()), Times.Once);
    }

    [Fact]
    public async Task GetRentalByIdAsync_ExistingId_ReturnsRental()
    {

        var rentalId = "RENT123";
        var rentalModel = new RentalModel
        {
            Identifier = rentalId,
            DeliverymanId = "DEL123",
            MotorcycleId = "MOTO123",
            DailyRate = 30.0m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            ExpectedEndDate = DateTime.UtcNow.Date.AddDays(7),
            Plan = 7
        };

        var rentalDto = new RentalDto
        {
            Identifier = rentalId,
            DeliverymanId = "DEL123",
            MotorcycleId = "MOTO123",
            DailyRate = 30.0m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            ExpectedEndDate = DateTime.UtcNow.Date.AddDays(7),
            Plan = 7
        };

        _mockRentalRepository.Setup(r => r.GetByIdentifierAsync(rentalId))
            .ReturnsAsync(rentalModel);

        _mockMapper.Setup(m => m.Map<RentalDto>(rentalModel))
            .Returns(rentalDto);


        var result = await _service.GetRentalByIdAsync(rentalId);


        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(rentalDto);
        _mockRentalRepository.Verify(r => r.GetByIdentifierAsync(rentalId), Times.Once);
    }

    [Fact]
    public async Task CalculateRentalCostAsync_EarlyReturn_AppliesPenalty()
    {

        var rentalId = "RENT123";
        var updateReturnDateDto = new UpdateReturnDateDto { ReturnDate = DateTime.UtcNow.Date.AddDays(5) };
        var rentalModel = new RentalModel
        {
            Identifier = rentalId,
            DailyRate = 30.0m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            ExpectedEndDate = DateTime.UtcNow.Date.AddDays(7),
            Plan = 7
        };

        _mockRentalRepository.Setup(r => r.GetByIdentifierAsync(rentalId))
            .ReturnsAsync(rentalModel);

        _mockRentalRepository.Setup(r => r.UpdateReturnDateAsync(rentalId, updateReturnDateDto.ReturnDate, It.IsAny<decimal>()))
            .Returns(Task.CompletedTask);


        var result = await _service.CalculateRentalCostAsync(rentalId, updateReturnDateDto);


        result.Should().NotBeNull();
        result.TotalCost.Should().Be(162.0m);
        result.Message.Should().Contain("Devolução antecipada");
        _mockRentalRepository.Verify(r => r.UpdateReturnDateAsync(rentalId, updateReturnDateDto.ReturnDate, It.IsAny<decimal>()), Times.Once);
    }

    [Fact]
    public async Task CalculateRentalCostAsync_LateReturn_AppliesExtraCharge()
    {

        var rentalId = "RENT123";
        var updateReturnDateDto = new UpdateReturnDateDto { ReturnDate = DateTime.UtcNow.Date.AddDays(9) };
        var rentalModel = new RentalModel
        {
            Identifier = rentalId,
            DailyRate = 30.0m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            ExpectedEndDate = DateTime.UtcNow.Date.AddDays(7),
            Plan = 7
        };

        _mockRentalRepository.Setup(r => r.GetByIdentifierAsync(rentalId))
            .ReturnsAsync(rentalModel);

        _mockRentalRepository.Setup(r => r.UpdateReturnDateAsync(rentalId, updateReturnDateDto.ReturnDate, It.IsAny<decimal>()))
            .Returns(Task.CompletedTask);


        var result = await _service.CalculateRentalCostAsync(rentalId, updateReturnDateDto);


        result.Should().NotBeNull();
        result.TotalCost.Should().Be(310.0m);
        result.Message.Should().Contain("Devolução atrasada");
        _mockRentalRepository.Verify(r => r.UpdateReturnDateAsync(rentalId, updateReturnDateDto.ReturnDate, It.IsAny<decimal>()), Times.Once);
    }

    [Fact]
    public async Task CreateRentalAsync_InvalidDeliverymanLicense_ThrowsArgumentException()
    {

        var createDto = new CreateRentalDto
        {
            Identifier = "RENT123",
            DeliverymanId = "DEL123",
            MotorcycleId = "MOTO123",
            Plan = 7
        };

        var deliverymanModel = new DeliverymanModel
        {
            Identifier = "DEL123",
            LicenseType = "B"
        };

        _mockCreateRentalValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateRentalDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _mockDeliverymanRepository.Setup(r => r.GetByIdentifierAsync(createDto.DeliverymanId))
            .ReturnsAsync(deliverymanModel);


        await _service.Invoking(s => s.CreateRentalAsync(createDto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("O entregador deve ter licença do tipo A ou AB");
    }

    // Adicione mais testes conforme necessário...
}