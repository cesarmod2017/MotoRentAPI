using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MotoRent.Application.DTOs.Deliveryman;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.Helpers;
using MotoRent.IntegrationTests.Models;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MotoRent.IntegrationTests.Controllers
{
    public class DeliverymanControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private string _token;

        public DeliverymanControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost:44382") // Ajuste para a URL correta
            });
        }

        // Método para gerar o token
        private async Task AuthenticateAsync()
        {
            var loginModel = new
            {
                username = "entregador",
                password = "password"
            };

            var response = await _client.PostAsJsonAsync("/login", loginModel);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            _token = result.Token;

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }



        [Fact]
        public async Task CreateDeliverymanAsync_ValidDto_ReturnsCreatedDeliveryman()
        {
            // Autentica e obtém o token
            await AuthenticateAsync();

            string base64Image = ImageHelper.ConvertImageToBase64("Assets\\cnh.png");

            var createDto = new CreateDeliverymanDto
            {
                Identifier = DeliverymanIdentifierGenerator.GenerateUniqueIdentifier(),
                Name = NameGenerator.GenerateFullName(),
                CNPJ = CnpjGenerator.GenerateCnpj(),
                BirthDate = new DateTime(1990, 1, 1),
                LicenseNumber = LicenseNumberGenerator.GenerateLicenseNumber(),
                LicenseType = "A",
                LicenseImage = $"data:image/png;base64,{base64Image}"
            };

            string json = JsonConvert.SerializeObject(createDto);

            var response = await _client.PostAsJsonAsync("/entregadores", createDto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task GetDeliverymanByIdAsync_ExistingId_ReturnsDeliveryman()
        {
            // Autentica e obtém o token
            await AuthenticateAsync();
            string base64Image = ImageHelper.ConvertImageToBase64("Assets\\cnh.png");
            var createDto = new CreateDeliverymanDto
            {
                Identifier = DeliverymanIdentifierGenerator.GenerateUniqueIdentifier(),
                Name = NameGenerator.GenerateFullName(),
                CNPJ = CnpjGenerator.GenerateCnpj(),
                BirthDate = new DateTime(1990, 1, 1),
                LicenseNumber = LicenseNumberGenerator.GenerateLicenseNumber(),
                LicenseType = "A",
                LicenseImage = $"data:image/png;base64,{base64Image}"
            };

            var createResponse = await _client.PostAsJsonAsync("/entregadores", createDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var response = await _client.GetAsync($"/entregadores/{createDto.Identifier}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var deliveryman = await response.Content.ReadFromJsonAsync<DeliverymanDto>();
            deliveryman.Should().NotBeNull();
            deliveryman.Identifier.Should().Be(createDto.Identifier);
            deliveryman.Name.Should().Be(createDto.Name);
        }

        [Fact]
        public async Task UpdateLicenseImageAsync_ValidIdAndDto_UpdatesLicenseImage()
        {
            // Autentica e obtém o token
            await AuthenticateAsync();
            string base64Image = ImageHelper.ConvertImageToBase64("Assets\\cnh.png");
            var createDto = new CreateDeliverymanDto
            {
                Identifier = DeliverymanIdentifierGenerator.GenerateUniqueIdentifier(),
                Name = NameGenerator.GenerateFullName(),
                CNPJ = CnpjGenerator.GenerateCnpj(),
                BirthDate = new DateTime(1985, 6, 15),
                LicenseNumber = LicenseNumberGenerator.GenerateLicenseNumber(),
                LicenseType = "B",
                LicenseImage = $"data:image/png;base64,{base64Image}"
            };

            // Cria um novo entregador
            var createResponse = await _client.PostAsJsonAsync("/entregadores", createDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);


            var updateDto = new UpdateLicenseImageDto
            {
                LicenseImage = $"data:image/png;base64,{base64Image}"
            };

            var response = await _client.PostAsJsonAsync($"/entregadores/{createDto.Identifier}/cnh", updateDto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Verifica se a imagem da licença foi atualizada corretamente
            var getResponse = await _client.GetAsync($"/entregadores/{createDto.Identifier}");
            var updatedDeliveryman = await getResponse.Content.ReadFromJsonAsync<DeliverymanDto>();
            updatedDeliveryman.Should().NotBeNull();
        }
    }
}
