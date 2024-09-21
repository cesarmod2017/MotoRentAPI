using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MotoRent.Application.DTOs.Deliveryman;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;
using MotoRent.Infrastructure.Storage;

namespace MotoRent.Application.Services
{
    public class DeliverymanService : IDeliverymanService
    {
        private readonly IDeliverymanRepository _deliverymanRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateDeliverymanDto> _createDeliverymanValidator;
        private readonly IValidator<UpdateLicenseImageDto> _updateLicenseImageValidator;
        private readonly ILogger<DeliverymanService> _logger;
        private readonly IFileStorageService _fileStorageService;
        public DeliverymanService(
            IDeliverymanRepository deliverymanRepository,
            IMapper mapper,
            IValidator<CreateDeliverymanDto> createDeliverymanValidator,
            IValidator<UpdateLicenseImageDto> updateLicenseImageValidator,
            ILogger<DeliverymanService> logger,
            IFileStorageService fileStorageService)
        {
            _deliverymanRepository = deliverymanRepository ?? throw new ArgumentNullException(nameof(deliverymanRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createDeliverymanValidator = createDeliverymanValidator ?? throw new ArgumentNullException(nameof(createDeliverymanValidator));
            _updateLicenseImageValidator = updateLicenseImageValidator ?? throw new ArgumentNullException(nameof(updateLicenseImageValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileStorageService = fileStorageService;
        }

        public async Task<DeliverymanDto> CreateDeliverymanAsync(CreateDeliverymanDto createDeliverymanDto)
        {

            _logger.LogInformation("Criando novo entregador: {@CreateDeliverymanDto}", createDeliverymanDto);

            var validationResult = await _createDeliverymanValidator.ValidateAsync(createDeliverymanDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Falha na validação para a solicitação de criação de entregador: {@ValidationErrors}", validationResult.Errors);
                throw new ValidationException(validationResult.Errors);
            }

            var existingDeliveryman = await _deliverymanRepository.GetByCNPJAsync(createDeliverymanDto.CNPJ);
            if (existingDeliveryman != null)
            {
                _logger.LogWarning("Tentativa de criar entregador com CNPJ existente: {CNPJ}", createDeliverymanDto.CNPJ);
                throw new ArgumentException("CNPJ já registrado");
            }

            existingDeliveryman = await _deliverymanRepository.GetByLicenseNumberAsync(createDeliverymanDto.LicenseNumber);
            if (existingDeliveryman != null)
            {
                _logger.LogWarning("Tentativa de criar entregador com número de licença existente: {LicenseNumber}", createDeliverymanDto.LicenseNumber);
                throw new ArgumentException("Número de licença já registrado");
            }

            createDeliverymanDto.LicenseImage = await SaveImageToStorage(createDeliverymanDto.Identifier, createDeliverymanDto.LicenseImage);


            var deliveryman = _mapper.Map<DeliverymanModel>(createDeliverymanDto);
            await _deliverymanRepository.CreateAsync(deliveryman);

            _logger.LogInformation("Entregador criado com sucesso: {@Deliveryman}", deliveryman);

            return _mapper.Map<DeliverymanDto>(deliveryman);
        }

        public async Task<DeliverymanDto> GetDeliverymanByIdAsync(string id)
        {
            _logger.LogInformation("Buscando entregador com ID: {DeliverymanId}", id);
            var deliveryman = await _deliverymanRepository.GetByFieldStringAsync("identifier", id);
            if (deliveryman == null)
            {
                _logger.LogWarning("Entregador não encontrado com ID: {DeliverymanId}", id);
                throw new ArgumentException("Entregador não encontrado", nameof(id));
            }
            return _mapper.Map<DeliverymanDto>(deliveryman);
        }

        public async Task UpdateLicenseImageAsync(string id, UpdateLicenseImageDto updateLicenseImageDto)
        {
            _logger.LogInformation("Atualizando imagem da licença para o entregador com ID: {DeliverymanId}", id);

            var validationResult = await _updateLicenseImageValidator.ValidateAsync(updateLicenseImageDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Falha na validação para a solicitação de atualização da imagem da licença: {@ValidationErrors}", validationResult.Errors);
                throw new ValidationException(validationResult.Errors);
            }

            var deliveryman = await _deliverymanRepository.GetByFieldStringAsync("identifier", id);
            if (deliveryman == null)
            {
                _logger.LogWarning("Entregador não encontrado com ID: {DeliverymanId}", id);
                throw new ArgumentException("Entregador não encontrado", nameof(id));
            }

            deliveryman.LicenseImage = await SaveImageToStorage(id, updateLicenseImageDto.LicenseImage);

            await _deliverymanRepository.UpdateLicenseImageAsync(id, deliveryman.LicenseImage);
            _logger.LogInformation("Imagem da licença atualizada com sucesso para o entregador com ID: {DeliverymanId}", id);
        }

        private async Task<string> SaveImageToStorage(string id, string base64Data)
        {
            string contentType;
            string fileExtension;

            if (base64Data.StartsWith("data:image/png;base64,"))
            {
                contentType = "image/png";
                fileExtension = "png";
                base64Data = base64Data.Substring("data:image/png;base64,".Length);
            }
            else if (base64Data.StartsWith("data:image/bmp;base64,"))
            {
                contentType = "image/bmp";
                fileExtension = "bmp";
                base64Data = base64Data.Substring("data:image/bmp;base64,".Length);
            }
            else
            {
                throw new ArgumentException("Formato de imagem inválido. Apenas PNG e BMP são aceitos.");
            }

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64Data);
                using var stream = new MemoryStream(imageBytes);

                string fileName = $"license_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}.{fileExtension}";

                string bucketName = "images-deliveryman-licences";

                try
                {
                    await _fileStorageService.EnsureBucketExists(bucketName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao verificar ou criar o bucket: {BucketName}", bucketName);
                    throw new InvalidOperationException("Não foi possível garantir a existência do bucket de armazenamento.", ex);
                }

                try
                {
                    await _fileStorageService.SetBucketPublicReadPolicy(bucketName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao definir a política de acesso público para o bucket: {BucketName}", bucketName);
                    throw new InvalidOperationException("Não foi possível definir a política de acesso para o bucket de armazenamento.", ex);
                }

                try
                {
                    await _fileStorageService.UploadFileAsync(bucketName, fileName, stream, contentType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao fazer upload do arquivo: {FileName} para o bucket: {BucketName}", fileName, bucketName);
                    throw new InvalidOperationException("Não foi possível fazer o upload da imagem para o serviço de armazenamento.", ex);
                }

                var imageUrl = _fileStorageService.GetPublicUrl(bucketName, fileName);
                return imageUrl;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Erro ao processar a imagem em base64");
                throw new ArgumentException("A string fornecida não é uma imagem codificada em base64 válida.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro não esperado ao salvar imagem no armazenamento");
                throw new Exception("Ocorreu um erro inesperado ao salvar a imagem.", ex);
            }
        }

        public async Task<string> GetRandomDeliverymanIdAsync()
        {
            var deliverymen = await _deliverymanRepository.GetAllAsync();
            var randomDeliveryman = deliverymen.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
            return randomDeliveryman?.Identifier;
        }
    }
}
