using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.Services;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;
using MotoRent.MessageConsumers.Services;

namespace MotoRent.UnitTests.Services
{
    public class MotorcycleServiceTests
    {
        private readonly Mock<IMotorcycleRepository> _mockRepository;
        private readonly Mock<IRentalRepository> _rentalRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<CreateMotorcycleDto>> _mockValidator;
        private readonly Mock<ILogger<MotorcycleService>> _mockLogger;
        private readonly MotorcycleService _service;
        private readonly Mock<IMessageService> _messageService;

        public MotorcycleServiceTests()
        {
            _mockRepository = new Mock<IMotorcycleRepository>();
            _rentalRepository = new Mock<IRentalRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockValidator = new Mock<IValidator<CreateMotorcycleDto>>();
            _mockLogger = new Mock<ILogger<MotorcycleService>>();
            _messageService = new Mock<IMessageService>();
            _service = new MotorcycleService(
                _mockRepository.Object,
                _mockMapper.Object,
                _mockValidator.Object,
                _mockLogger.Object,
                _messageService.Object, _rentalRepository.Object
            );
        }

        [Fact]
        public async Task CreateMotorcycleAsync_ValidDto_ReturnsCreatedMotorcycle()
        {
            var identifier = "MOTO123";
            var year = 2023;
            var model = "TestModel";
            var licensePlate = "ABC-1234";

            var createDto = new CreateMotorcycleDto
            {
                Identifier = identifier,
                Year = year,
                Model = model,
                LicensePlate = licensePlate
            };

            var motorcycleModel = new MotorcycleModel
            {
                Identifier = identifier,
                Year = year,
                Model = model,
                LicensePlate = licensePlate
            };

            var motorcycleDto = new MotorcycleDto
            {
                Identifier = identifier,
                Year = year,
                Model = model,
                LicensePlate = licensePlate
            };

            _mockValidator.Setup(v => v.ValidateAsync(createDto, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockMapper.Setup(m => m.Map<MotorcycleModel>(createDto)).Returns(motorcycleModel);
            _mockMapper.Setup(m => m.Map<MotorcycleDto>(motorcycleModel)).Returns(motorcycleDto);

            _mockRepository.Setup(r => r.CreateAsync(motorcycleModel)).ReturnsAsync(motorcycleModel);

            var result = await _service.CreateMotorcycleAsync(createDto);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(motorcycleDto, options => options.ExcludingMissingMembers());

            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<MotorcycleModel>()), Times.Once);
            _mockValidator.Verify(v => v.ValidateAsync(createDto, default), Times.Once);
        }


        [Fact]
        public async Task GetMotorcycleByIdAsync_ExistingId_ReturnsMotorcycle()
        {
            var motorcycleId = "1";
            var identifier = "MOTO123";
            var year = 2023;
            var model = "TestModel";
            var licensePlate = "ABC-1234";

            var motorcycleModel = new MotorcycleModel
            {
                Id = motorcycleId,
                Identifier = identifier,
                Year = year,
                Model = model,
                LicensePlate = licensePlate
            };

            var motorcycleDto = new MotorcycleDto
            {
                Identifier = identifier,
                Year = year,
                Model = model,
                LicensePlate = licensePlate
            };

            _mockRepository.Setup(r => r.GetByIdAsync(motorcycleId))
                .ReturnsAsync(motorcycleModel);

            _mockMapper.Setup(m => m.Map<MotorcycleDto>(motorcycleModel))
                .Returns(motorcycleDto);

            var result = await _service.GetMotorcycleByIdAsync(motorcycleId);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(motorcycleDto, options => options.ExcludingMissingMembers());

            _mockRepository.Verify(r => r.GetByIdAsync(motorcycleId), Times.Once);
        }


        [Fact]
        public async Task GetMotorcycleByIdAsync_NonExistingId_ThrowsArgumentException()
        {
            var nonExistingId = "999";

            _mockRepository.Setup(r => r.GetByIdAsync(nonExistingId))
                .ReturnsAsync((MotorcycleModel)null);

            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.GetMotorcycleByIdAsync(nonExistingId));

            exception.Message.Should().Contain("Motorcycle not found");

            _mockRepository.Verify(r => r.GetByIdAsync(nonExistingId), Times.Once);
        }


        [Fact]
        public async Task GetAllMotorcyclesAsync_ReturnsAllMotorcycles()
        {
            var motorcycles = new List<MotorcycleModel>
    {
        new MotorcycleModel { Id = "1", Identifier = "MOTO1", Year = 2023, Model = "Model1", LicensePlate = "ABC-1234" },
        new MotorcycleModel { Id = "2", Identifier = "MOTO2", Year = 2022, Model = "Model2", LicensePlate = "DEF-5678" }
    };

            var motorcycleDtos = new List<MotorcycleDto>
    {
        new MotorcycleDto { Identifier = "MOTO1", Year = 2023, Model = "Model1", LicensePlate = "ABC-1234" },
        new MotorcycleDto { Identifier = "MOTO2", Year = 2022, Model = "Model2", LicensePlate = "DEF-5678" }
    };

            _mockRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(motorcycles);

            _mockMapper.Setup(m => m.Map<IEnumerable<MotorcycleDto>>(motorcycles))
                .Returns(motorcycleDtos);

            var result = await _service.GetAllMotorcyclesAsync();

            result.Should().NotBeNull();
            result.Should().HaveCount(motorcycleDtos.Count);
            result.Should().BeEquivalentTo(motorcycleDtos);

            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }


        [Fact]
        public async Task UpdateMotorcycleLicensePlateAsync_ValidId_UpdatesLicensePlate()
        {
            var motorcycleId = "1";
            var oldLicensePlate = "ABC-1234";
            var newLicensePlate = "XYZ-9876";

            var updateLicensePlateDto = new UpdateLicensePlateDto { LicensePlate = newLicensePlate };

            var existingMotorcycle = new MotorcycleModel
            {
                Id = motorcycleId,
                Identifier = "MOTO1",
                Year = 2023,
                Model = "TestModel",
                LicensePlate = oldLicensePlate
            };

            _mockRepository.Setup(r => r.GetByIdAsync(motorcycleId))
                .ReturnsAsync(existingMotorcycle);

            _mockRepository.Setup(r => r.UpdateAsync(motorcycleId, It.IsAny<MotorcycleModel>()))
                .Returns(Task.CompletedTask);

            await _service.UpdateMotorcycleLicensePlateAsync(motorcycleId, updateLicensePlateDto);

            _mockRepository.Verify(r => r.GetByIdAsync(motorcycleId), Times.Once);

            _mockRepository.Verify(r => r.UpdateAsync(motorcycleId,
                It.Is<MotorcycleModel>(m =>
                    m.LicensePlate == newLicensePlate)), Times.Once);
        }


        [Fact]
        public async Task UpdateMotorcycleLicensePlateAsync_InvalidId_ThrowsArgumentException()
        {
            var invalidId = "999";
            var newLicensePlate = "XYZ-9876";
            var updateLicensePlateDto = new UpdateLicensePlateDto { LicensePlate = newLicensePlate };

            _mockRepository.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((MotorcycleModel)null);

            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.UpdateMotorcycleLicensePlateAsync(invalidId, updateLicensePlateDto));

            exception.Message.Should().Contain("Motorcycle not found");

            _mockRepository.Verify(r => r.GetByIdAsync(invalidId), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<MotorcycleModel>()), Times.Never);
        }


        [Fact]
        public async Task DeleteMotorcycleAsync_ExistingId_DeletesMotorcycle()
        {
            var motorcycleId = "1";

            _mockRepository.Setup(r => r.DeleteAsync(motorcycleId))
                .Returns(Task.CompletedTask);

            await _service.DeleteMotorcycleAsync(motorcycleId);

            _mockRepository.Verify(r => r.DeleteAsync(motorcycleId), Times.Once);
        }


        [Fact]
        public async Task GetMotorcyclesByLicensePlateAsync_ExistingLicensePlate_ReturnsMotorcycles()
        {
            var licensePlate = "ABC-1234";
            var motorcycles = new List<MotorcycleModel>
    {
        new MotorcycleModel { Id = "1", Identifier = "MOTO1", Year = 2023, Model = "Model1", LicensePlate = licensePlate }
    };

            var motorcycleDtos = new List<MotorcycleDto>
    {
        new MotorcycleDto { Identifier = "MOTO1", Year = 2023, Model = "Model1", LicensePlate = licensePlate }
    };

            _mockRepository.Setup(r => r.GetByLicensePlateAsync(licensePlate))
                .ReturnsAsync(motorcycles);

            _mockMapper.Setup(m => m.Map<IEnumerable<MotorcycleDto>>(motorcycles))
                .Returns(motorcycleDtos);

            var result = await _service.GetMotorcyclesByLicensePlateAsync(licensePlate);

            result.Should().NotBeNull();
            result.Should().HaveCount(motorcycleDtos.Count);
            result.Should().BeEquivalentTo(motorcycleDtos);

            _mockRepository.Verify(r => r.GetByLicensePlateAsync(licensePlate), Times.Once);
        }

    }
}