using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.Helpers;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.IntegrationTests.Models;
using MotoRent.MessageConsumers.Events;
using MotoRent.MessageConsumers.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MotoRent.IntegrationTests.Controllers
{
    public class MotorcyclesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private string _token;
        private readonly IMessageService _messageService;
        private readonly INotificationRepository _notificationRepository;

        public MotorcyclesControllerTests(CustomWebApplicationFactory<Program> factory, IMessageService messageService, INotificationRepository notificationRepository)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost:44382") // Certifique-se de usar a URL correta
            });
            _messageService = messageService;
            _notificationRepository = notificationRepository;
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

        [Fact]
        public async Task MotorcycleCreatedEvent_Should_Be_Consumed_And_Saved()
        {

            var motorcycleCreatedEvent = new MotorcycleCreatedEvent
            {
                Id = "1",
                Identifier = "MOTO2024",
                Year = 2024,
                Model = "TestModel",
                LicensePlate = "ABC-1234"
            };


            await _messageService.PublishAsync("motorcycle-created", motorcycleCreatedEvent);


            // Aguarde um pouco para dar tempo ao consumidor de processar a mensagem
            await Task.Delay(2000);

            // Verifique se a notificação foi salva no banco de dados
            var notifications = await _notificationRepository.GetAllAsync();
            var savedNotification = notifications.FirstOrDefault(n => n.Message.Contains("TestModel") && n.Message.Contains("ABC-1234"));

            Assert.NotNull(savedNotification);
            Assert.Contains("New 2024 motorcycle created", savedNotification.Message);
        }
    }
}
