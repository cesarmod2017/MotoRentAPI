using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.Helpers;
using MotoRent.IntegrationTests.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MotoRent.IntegrationTests.Controllers
{
    public class MotorcyclesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private string _token;

        public MotorcyclesControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost:44382") // Certifique-se de usar a URL correta
            });
        }

        // Método para gerar o token
        private async Task AuthenticateAsync()
        {
            var loginModel = new
            {
                username = "admin",
                password = "password"
            };

            var response = await _client.PostAsJsonAsync("/login", loginModel);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            _token = result.Token;

            // Adiciona o token às requisições futuras
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }


        [Fact]
        public async Task CreateMotorcycle_ReturnsCreatedMotorcycle()
        {
            // Autentica e obtém o token
            await AuthenticateAsync();

            var createDto = MotorcycleDtoGenerator.Generate();

            var response = await _client.PostAsJsonAsync("/motos", createDto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task GetMotorcycle_ReturnsMotorcycle()
        {
            // Autentica e obtém o token
            await AuthenticateAsync();

            var createDto = MotorcycleDtoGenerator.Generate();

            var createResponse = await _client.PostAsJsonAsync("/motos", createDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var response = await _client.GetAsync($"/motos/{createDto.Identifier}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var motorcycle = await response.Content.ReadFromJsonAsync<MotorcycleDto>();
            motorcycle.Should().NotBeNull();
            motorcycle.Identifier.Should().Be(createDto.Identifier);
            motorcycle.Year.Should().Be(createDto.Year);
            motorcycle.Model.Should().Be(createDto.Model);
            motorcycle.LicensePlate.Should().Be(createDto.LicensePlate);
        }

        [Fact]
        public async Task UpdateMotorcycleLicensePlate_ReturnsOk()
        {
            // Autentica e obtém o token
            await AuthenticateAsync();

            var createDto = MotorcycleDtoGenerator.Generate();
            var createResponse = await _client.PostAsJsonAsync("/motos", createDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var newLicense = MotorcycleDtoGenerator.Generate();


            var updateLicensePlateDto = new UpdateLicensePlateDto { LicensePlate = newLicense.LicensePlate };

            var response = await _client.PutAsJsonAsync($"/motos/{createDto.Identifier}/placa", updateLicensePlateDto);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var getResponse = await _client.GetAsync($"/motos/{createDto.Identifier}");
            var updatedMotorcycle = await getResponse.Content.ReadFromJsonAsync<MotorcycleDto>();
            updatedMotorcycle.Should().NotBeNull();
            updatedMotorcycle.LicensePlate.Should().Be(updateLicensePlateDto.LicensePlate);
        }
    }
}
