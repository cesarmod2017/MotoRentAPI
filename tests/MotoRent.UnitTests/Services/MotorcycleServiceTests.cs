using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.Services;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;
using MotoRent.MessageConsumers.Events;
using MotoRent.MessageConsumers.Services;

namespace MotoRent.UnitTests.Services;

public class MotorcycleServiceTests
{
    private readonly Mock<IMotorcycleRepository> _mockRepository;
    private readonly Mock<IRentalRepository> _mockRentalRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IValidator<CreateMotorcycleDto>> _mockValidator;
    private readonly Mock<ILogger<MotorcycleService>> _mockLogger;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly MotorcycleService _service;

    public MotorcycleServiceTests()
    {
        _mockRepository = new Mock<IMotorcycleRepository>();
        _mockRentalRepository = new Mock<IRentalRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockValidator = new Mock<IValidator<CreateMotorcycleDto>>();
        _mockLogger = new Mock<ILogger<MotorcycleService>>();
        _mockMessageService = new Mock<IMessageService>();
        _service = new MotorcycleService(
            _mockRepository.Object,
            _mockMapper.Object,
            _mockValidator.Object,
            _mockLogger.Object,
            _mockMessageService.Object,
            _mockRentalRepository.Object
        );
    }

    [Fact]
    public async Task CreateMotorcycleAsync_ValidDto_ReturnsCreatedMotorcycle()
    {

        var createDto = new CreateMotorcycleDto
        {
            Identifier = "MOTO123",
            Year = 2023,
            Model = "TestModel",
            LicensePlate = "ABC-1234"
        };

        var motorcycleModel = new MotorcycleModel
        {
            Identifier = "MOTO123",
            Year = 2023,
            Model = "TestModel",
            LicensePlate = "ABC-1234"
        };

        var motorcycleDto = new MotorcycleDto
        {
            Identifier = "MOTO123",
            Year = 2023,
            Model = "TestModel",
            LicensePlate = "ABC-1234"
        };

        _mockValidator.Setup(v => v.ValidateAsync(createDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockMapper.Setup(m => m.Map<MotorcycleModel>(createDto)).Returns(motorcycleModel);
        _mockMapper.Setup(m => m.Map<MotorcycleDto>(motorcycleModel)).Returns(motorcycleDto);

        _mockRepository.Setup(r => r.CreateAsync(motorcycleModel)).ReturnsAsync(motorcycleModel);

        _mockMessageService.Setup(m => m.PublishAsync(It.IsAny<string>(), It.IsAny<MotorcycleCreatedEvent>()))
            .Returns(Task.CompletedTask);


        var result = await _service.CreateMotorcycleAsync(createDto);


        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(motorcycleDto);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<MotorcycleModel>()), Times.Once);
        _mockMessageService.Verify(m => m.PublishAsync(It.IsAny<string>(), It.IsAny<MotorcycleCreatedEvent>()), Times.Once);
    }

    [Fact]
    public async Task GetMotorcycleByIdAsync_ExistingId_ReturnsMotorcycle()
    {

        var motorcycleId = "MOTO123";
        var motorcycleModel = new MotorcycleModel
        {
            Identifier = motorcycleId,
            Year = 2023,
            Model = "TestModel",
            LicensePlate = "ABC-1234"
        };

        var motorcycleDto = new MotorcycleDto
        {
            Identifier = motorcycleId,
            Year = 2023,
            Model = "TestModel",
            LicensePlate = "ABC-1234"
        };

        _mockRepository.Setup(r => r.GetByFieldStringAsync("identifier", motorcycleId))
            .ReturnsAsync(motorcycleModel);

        _mockMapper.Setup(m => m.Map<MotorcycleDto>(motorcycleModel))
            .Returns(motorcycleDto);


        var result = await _service.GetMotorcycleByIdAsync(motorcycleId);


        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(motorcycleDto);
        _mockRepository.Verify(r => r.GetByFieldStringAsync("identifier", motorcycleId), Times.Once);
    }


    [Fact]
    public async Task CreateMotorcycleAsync_ValidDto_CreatesMotorcycleAndPublishesEvent()
    {

        var createDto = new CreateMotorcycleDto
        {
            Identifier = "MOTO123",
            Year = 2023,
            Model = "TestModel",
            LicensePlate = "ABC-1234"
        };

        var motorcycleModel = new MotorcycleModel
        {
            Identifier = "MOTO123",
            Year = 2023,
            Model = "TestModel",
            LicensePlate = "ABC-1234"
        };

        _mockValidator.Setup(v => v.ValidateAsync(createDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockRepository.Setup(r => r.GetByLicensePlateAsync(createDto.LicensePlate))
            .ReturnsAsync(new List<MotorcycleModel>());

        _mockMapper.Setup(m => m.Map<MotorcycleModel>(createDto)).Returns(motorcycleModel);
        _mockMapper.Setup(m => m.Map<MotorcycleDto>(motorcycleModel)).Returns(new MotorcycleDto());

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<MotorcycleModel>()))
            .ReturnsAsync(motorcycleModel);


        var result = await _service.CreateMotorcycleAsync(createDto);


        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<MotorcycleModel>()), Times.Once);
        _mockMessageService.Verify(m => m.PublishAsync("motorcycle-created", It.IsAny<MotorcycleCreatedEvent>()), Times.Once);
    }

    [Fact]
    public async Task CreateMotorcycleAsync_DuplicateLicensePlate_ThrowsArgumentException()
    {

        var createDto = new CreateMotorcycleDto
        {
            Identifier = "MOTO123",
            Year = 2023,
            Model = "TestModel",
            LicensePlate = "ABC-1234"
        };

        _mockValidator.Setup(v => v.ValidateAsync(createDto, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockRepository.Setup(r => r.GetByLicensePlateAsync(createDto.LicensePlate))
            .ReturnsAsync(new List<MotorcycleModel> { new MotorcycleModel() });


        await _service.Invoking(s => s.CreateMotorcycleAsync(createDto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Já existe uma moto cadastrada com esta placa. Por favor, verifique e tente novamente com uma placa diferente.");
    }

    [Fact]
    public async Task GetAllMotorcyclesAsync_WithLicensePlateFilter_ReturnsFilteredMotorcycles()
    {

        var licensePlate = "ABC-1234";
        var motorcycles = new List<MotorcycleModel>
        {
            new MotorcycleModel { Identifier = "MOTO123", LicensePlate = licensePlate },
            new MotorcycleModel { Identifier = "MOTO456", LicensePlate = "XYZ-5678" }
        };

        _mockRepository.Setup(r => r.GetByLicensePlateAsync(licensePlate))
            .ReturnsAsync(motorcycles.Where(m => m.LicensePlate == licensePlate).ToList());

        _mockMapper.Setup(m => m.Map<IEnumerable<MotorcycleDto>>(It.IsAny<IEnumerable<MotorcycleModel>>()))
            .Returns((IEnumerable<MotorcycleModel> models) => models.Select(model => new MotorcycleDto { Identifier = model.Identifier, LicensePlate = model.LicensePlate }));


        var result = await _service.GetMotorcyclesByLicensePlateAsync(licensePlate);


        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().LicensePlate.Should().Be(licensePlate);
    }

    [Fact]
    public async Task UpdateMotorcycleLicensePlateAsync_ValidData_UpdatesLicensePlate()
    {

        var motorcycleId = "MOTO123";
        var updateDto = new UpdateLicensePlateDto { LicensePlate = "XYZ-5678" };
        var existingMotorcycle = new MotorcycleModel
        {
            Identifier = motorcycleId,
            LicensePlate = "ABC-1234"
        };

        _mockRepository.Setup(r => r.GetByFieldStringAsync("identifier", motorcycleId))
            .ReturnsAsync(existingMotorcycle);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<MotorcycleModel>()))
            .Returns(Task.CompletedTask);


        await _service.UpdateMotorcycleLicensePlateAsync(motorcycleId, updateDto);


        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.Is<MotorcycleModel>(m => m.LicensePlate == updateDto.LicensePlate)), Times.Once);
    }

    [Fact]
    public async Task DeleteMotorcycleAsync_NoAssociatedRentals_DeletesMotorcycle()
    {

        var motorcycleId = "MOTO123";
        var existingMotorcycle = new MotorcycleModel { Identifier = motorcycleId };

        _mockRepository.Setup(r => r.GetByFieldStringAsync("identifier", motorcycleId))
            .ReturnsAsync(existingMotorcycle);

        _mockRentalRepository.Setup(r => r.ExistsForMotorcycleAsync(motorcycleId))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);


        await _service.DeleteMotorcycleAsync(motorcycleId);


        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMotorcycleAsync_WithAssociatedRentals_ThrowsInvalidOperationException()
    {

        var motorcycleId = "MOTO123";
        var existingMotorcycle = new MotorcycleModel { Identifier = motorcycleId };

        _mockRepository.Setup(r => r.GetByFieldStringAsync("identifier", motorcycleId))
            .ReturnsAsync(existingMotorcycle);

        _mockRentalRepository.Setup(r => r.ExistsForMotorcycleAsync(motorcycleId))
            .ReturnsAsync(true);


        await _service.Invoking(s => s.DeleteMotorcycleAsync(motorcycleId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Não é possível remover a moto pois existem locações associadas a ela.");
    }
}