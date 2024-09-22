using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using MotoRent.Application.DTOs.Deliveryman;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.Services;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;
using MotoRent.Infrastructure.Storage;

namespace MotoRent.UnitTests.Services;
public class DeliverymanServiceTests
{
    private readonly Mock<IDeliverymanRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IValidator<CreateDeliverymanDto>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateLicenseImageDto>> _mockUpdateValidator;
    private readonly Mock<ILogger<DeliverymanService>> _mockLogger;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly DeliverymanService _service;

    public DeliverymanServiceTests()
    {
        _mockRepository = new Mock<IDeliverymanRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockCreateValidator = new Mock<IValidator<CreateDeliverymanDto>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateLicenseImageDto>>();
        _mockLogger = new Mock<ILogger<DeliverymanService>>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _service = new DeliverymanService(
            _mockRepository.Object,
            _mockMapper.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object,
            _mockLogger.Object,
            _mockFileStorageService.Object
        );
    }

    [Fact]
    public async Task CreateDeliverymanAsync_ValidDto_ReturnsCreatedDeliveryman()
    {

        var createDto = new CreateDeliverymanDto
        {
            Identifier = "DEL123",
            Name = "John Doe",
            CNPJ = "12345678901234",
            BirthDate = new DateTime(1990, 1, 1),
            LicenseNumber = "12345678901",
            LicenseType = "A",
            LicenseImage = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="
        };

        var deliverymanModel = new DeliverymanModel
        {
            Identifier = "DEL123",
            Name = "John Doe",
            CNPJ = "12345678901234",
            BirthDate = new DateTime(1990, 1, 1),
            LicenseNumber = "12345678901",
            LicenseType = "A",
            LicenseImage = "https://example.com/image.png"
        };

        _mockCreateValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateDeliverymanDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _mockRepository.Setup(r => r.GetByCNPJAsync(It.IsAny<string>())).ReturnsAsync((DeliverymanModel)null);
        _mockRepository.Setup(r => r.GetByLicenseNumberAsync(It.IsAny<string>())).ReturnsAsync((DeliverymanModel)null);

        _mockMapper.Setup(m => m.Map<DeliverymanModel>(createDto)).Returns(deliverymanModel);
        _mockMapper.Setup(m => m.Map<DeliverymanDto>(deliverymanModel)).Returns(new DeliverymanDto());

        _mockFileStorageService.Setup(f => f.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("https://example.com/image.png");

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<DeliverymanModel>()))
            .ReturnsAsync(deliverymanModel);


        var result = await _service.CreateDeliverymanAsync(createDto);


        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<DeliverymanModel>()), Times.Once);
        _mockFileStorageService.Verify(f => f.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateDeliverymanAsync_InvalidLicenseType_ThrowsValidationException()
    {

        var createDto = new CreateDeliverymanDto
        {
            Identifier = "DEL123",
            Name = "John Doe",
            CNPJ = "12345678901234",
            BirthDate = new DateTime(1990, 1, 1),
            LicenseNumber = "12345678901",
            LicenseType = "C", // Invalid license type
            LicenseImage = "data:image/png;base64,..."
        };

        _mockCreateValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateDeliverymanDto>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("LicenseType", "O tipo de licença deve ser A, B ou AB") }));


        await _service.Invoking(s => s.CreateDeliverymanAsync(createDto))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*O tipo de licença deve ser A, B ou AB*");
    }

    [Fact]
    public async Task CreateDeliverymanAsync_DuplicateCNPJ_ThrowsArgumentException()
    {

        var createDto = new CreateDeliverymanDto
        {
            Identifier = "DEL123",
            Name = "John Doe",
            CNPJ = "12345678901234",
            BirthDate = new DateTime(1990, 1, 1),
            LicenseNumber = "12345678901",
            LicenseType = "A",
            LicenseImage = "data:image/png;base64,..."
        };

        _mockCreateValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateDeliverymanDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _mockRepository.Setup(r => r.GetByCNPJAsync(createDto.CNPJ))
            .ReturnsAsync(new DeliverymanModel());


        await _service.Invoking(s => s.CreateDeliverymanAsync(createDto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("CNPJ já registrado");
    }

    [Fact]
    public async Task CreateDeliverymanAsync_DuplicateLicenseNumber_ThrowsArgumentException()
    {

        var createDto = new CreateDeliverymanDto
        {
            Identifier = "DEL123",
            Name = "John Doe",
            CNPJ = "12345678901234",
            BirthDate = new DateTime(1990, 1, 1),
            LicenseNumber = "12345678901",
            LicenseType = "A",
            LicenseImage = "data:image/png;base64,..."
        };

        _mockCreateValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateDeliverymanDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _mockRepository.Setup(r => r.GetByCNPJAsync(It.IsAny<string>())).ReturnsAsync((DeliverymanModel)null);
        _mockRepository.Setup(r => r.GetByLicenseNumberAsync(createDto.LicenseNumber))
            .ReturnsAsync(new DeliverymanModel());


        await _service.Invoking(s => s.CreateDeliverymanAsync(createDto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Número de licença já registrado");
    }

    [Fact]
    public async Task UpdateLicenseImageAsync_ValidImage_UpdatesLicenseImage()
    {

        var deliverymanId = "DEL123";
        var updateDto = new UpdateLicenseImageDto
        {
            LicenseImage = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="
        };

        var existingDeliveryman = new DeliverymanModel
        {
            Identifier = deliverymanId,
            LicenseImage = "https://example.com/old-image.png"
        };

        _mockUpdateValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateLicenseImageDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _mockRepository.Setup(r => r.GetByFieldStringAsync("identifier", deliverymanId))
            .ReturnsAsync(existingDeliveryman);

        _mockFileStorageService.Setup(f => f.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("https://example.com/new-image.png");

        _mockRepository.Setup(r => r.UpdateLicenseImageAsync(deliverymanId, It.IsAny<string>()))
            .Returns(Task.CompletedTask);


        await _service.UpdateLicenseImageAsync(deliverymanId, updateDto);


        _mockFileStorageService.Verify(f => f.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateLicenseImageAsync(deliverymanId, "https://example.com/new-image.png"), Times.Once);
    }

    [Fact]
    public async Task UpdateLicenseImageAsync_InvalidImageFormat_ThrowsArgumentException()
    {

        var deliverymanId = "DEL123";
        var updateDto = new UpdateLicenseImageDto
        {
            LicenseImage = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAIBAQIBAQICAgICAgICAwUDAwMDAwYEBAMFBwYHBwcGBwcICQsJCAgKCAcHCg0KCgsMDAwMBwkODw0MDgsMDAz/2wBDAQICAgMDAwYDAwYMCAcIDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAz/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD7TooooA//2Q=="
        };

        _mockUpdateValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateLicenseImageDto>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("LicenseImage", "A imagem da licença deve estar em formato PNG ou BMP.") }));


        await _service.Invoking(s => s.UpdateLicenseImageAsync(deliverymanId, updateDto))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*A imagem da licença deve estar em formato PNG ou BMP*");
    }
}