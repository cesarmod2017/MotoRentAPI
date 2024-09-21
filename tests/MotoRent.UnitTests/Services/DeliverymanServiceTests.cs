using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using MotoRent.Application.DTOs.Deliveryman;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.Helpers;
using MotoRent.Application.Services;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;
using MotoRent.Infrastructure.Storage;

namespace MotoRent.UnitTests.Services
{
    public class DeliverymanServiceTests
    {
        private readonly Mock<IDeliverymanRepository> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<CreateDeliverymanDto>> _mockCreateValidator;
        private readonly Mock<IValidator<UpdateLicenseImageDto>> _mockUpdateValidator;
        private readonly Mock<ILogger<DeliverymanService>> _mockLogger;
        private readonly DeliverymanService _service;
        private readonly Mock<IFileStorageService> _fileStorageService;
        public DeliverymanServiceTests()
        {
            _mockRepository = new Mock<IDeliverymanRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCreateValidator = new Mock<IValidator<CreateDeliverymanDto>>();
            _mockUpdateValidator = new Mock<IValidator<UpdateLicenseImageDto>>();
            _mockLogger = new Mock<ILogger<DeliverymanService>>();
            _fileStorageService = new Mock<IFileStorageService>();
            _service = new DeliverymanService(
                    _mockRepository.Object,
                    _mockMapper.Object,
                    _mockCreateValidator.Object,
                    _mockUpdateValidator.Object,
                    _mockLogger.Object,
                    _fileStorageService.Object
                );
        }

        [Fact]
        public async Task CreateDeliverymanAsync_ValidDto_ReturnsCreatedDeliveryman()
        {
            var createDto = new CreateDeliverymanDto
            {
                Identifier = "DEL123",
                Name = "John Doe",
                CNPJ = CnpjGenerator.GenerateCnpj(),
                BirthDate = new DateTime(1990, 1, 1),
                LicenseNumber = LicenseNumberGenerator.GenerateLicenseNumber(),
                LicenseType = "A",
                LicenseImage = "base64image"
            };

            var deliverymanModel = new DeliverymanModel
            {
                Id = "1",
                Identifier = "DEL123",
                Name = "John Doe",
                CNPJ = CnpjGenerator.GenerateCnpj(),
                BirthDate = new DateTime(1990, 1, 1),
                LicenseNumber = LicenseNumberGenerator.GenerateLicenseNumber(),
                LicenseType = "A",
                LicenseImage = "base64image"
            };

            var deliverymanDto = new DeliverymanDto
            {
                Id = "1",
                Identifier = "DEL123",
                Name = "John Doe",
                CNPJ = CnpjGenerator.GenerateCnpj(),
                BirthDate = new DateTime(1990, 1, 1),
                LicenseNumber = LicenseNumberGenerator.GenerateLicenseNumber(),
                LicenseType = "A",
                LicenseImage = "base64image"
            };

            _mockCreateValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateDeliverymanDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockRepository.Setup(r => r.GetByCNPJAsync(It.IsAny<string>())).ReturnsAsync((DeliverymanModel)null);
            _mockRepository.Setup(r => r.GetByLicenseNumberAsync(It.IsAny<string>())).ReturnsAsync((DeliverymanModel)null);

            _mockMapper.Setup(m => m.Map<DeliverymanModel>(createDto)).Returns(deliverymanModel);
            _mockMapper.Setup(m => m.Map<DeliverymanDto>(deliverymanModel)).Returns(deliverymanDto);

            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<DeliverymanModel>()))
                .ReturnsAsync(deliverymanModel);

            var result = await _service.CreateDeliverymanAsync(createDto);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(deliverymanDto);
            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<DeliverymanModel>()), Times.Once);
        }

        [Fact]
        public async Task CreateDeliverymanAsync_ExistingCNPJ_ThrowsArgumentException()
        {
            var createDto = new CreateDeliverymanDto
            {
                CNPJ = CnpjGenerator.GenerateCnpj(),
            };

            _mockCreateValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateDeliverymanDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockRepository.Setup(r => r.GetByCNPJAsync(createDto.CNPJ))
                .ReturnsAsync(new DeliverymanModel());

            await _service.Invoking(s => s.CreateDeliverymanAsync(createDto))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("CNPJ already registered");
        }

        [Fact]
        public async Task GetDeliverymanByIdAsync_ExistingId_ReturnsDeliveryman()
        {
            var deliverymanId = "1";
            var deliverymanModel = new DeliverymanModel
            {
                Id = deliverymanId,
                Identifier = "DEL123",
                Name = "John Doe",
            };

            var deliverymanDto = new DeliverymanDto
            {
                Id = deliverymanId,
                Identifier = "DEL123",
                Name = "John Doe",
            };

            _mockRepository.Setup(r => r.GetByIdAsync(deliverymanId))
                .ReturnsAsync(deliverymanModel);

            _mockMapper.Setup(m => m.Map<DeliverymanDto>(deliverymanModel))
                .Returns(deliverymanDto);

            var result = await _service.GetDeliverymanByIdAsync(deliverymanId);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(deliverymanDto);
            _mockRepository.Verify(r => r.GetByIdAsync(deliverymanId), Times.Once);
        }

        [Fact]
        public async Task UpdateLicenseImageAsync_ValidIdAndDto_UpdatesLicenseImage()
        {
            var deliverymanId = "1";
            var updateDto = new UpdateLicenseImageDto
            {
                LicenseImage = "newbase64image"
            };

            var existingDeliveryman = new DeliverymanModel
            {
                Id = deliverymanId,
            };

            _mockUpdateValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateLicenseImageDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockRepository.Setup(r => r.GetByIdAsync(deliverymanId))
                .ReturnsAsync(existingDeliveryman);

            _mockRepository.Setup(r => r.UpdateLicenseImageAsync(deliverymanId, updateDto.LicenseImage))
                .Returns(Task.CompletedTask);

            await _service.UpdateLicenseImageAsync(deliverymanId, updateDto);

            _mockRepository.Verify(r => r.UpdateLicenseImageAsync(deliverymanId, updateDto.LicenseImage), Times.Once);
        }

        [Fact]
        public async Task UpdateLicenseImageAsync_NonExistingId_ThrowsArgumentException()
        {
            var nonExistingId = "999";
            var updateDto = new UpdateLicenseImageDto
            {
                LicenseImage = "newbase64image"
            };

            _mockUpdateValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateLicenseImageDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockRepository.Setup(r => r.GetByIdAsync(nonExistingId))
                .ReturnsAsync((DeliverymanModel)null);

            await _service.Invoking(s => s.UpdateLicenseImageAsync(nonExistingId, updateDto))
               .Should().ThrowAsync<ArgumentException>()
               .WithMessage("Deliveryman not found*");
        }
    }
}