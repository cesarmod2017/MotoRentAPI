using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MotoRent.Application.DTOs.Rental;
using MotoRent.Application.Helpers;
using MotoRent.Application.Services;
using MotoRent.IntegrationTests.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MotoRent.IntegrationTests.Controllers
{
    public class RentalsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;
        private string _token;

        public RentalsControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost:44382")
            });
        }
        private async Task<string> GetRandomDeliverymanIdAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var deliverymanService = scope.ServiceProvider.GetRequiredService<IDeliverymanService>();
                return await deliverymanService.GetRandomDeliverymanIdAsync();
            }
        }

        private async Task<string> GetRandomMotorcycleIdAsync()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var motorcycleService = scope.ServiceProvider.GetRequiredService<IMotorcycleService>();
                return await motorcycleService.GetRandomMotorcycleIdAsync();
            }
        }


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
        public async Task CreateRental_ReturnsCreatedRental()
        {
            await AuthenticateAsync();
            var randomPlan = TestDataGenerator.GetRandomPlan();
            var createRentalDto = new CreateRentalDto
            {
                Identifier = TestDataGenerator.GenerateUniqueIdentifier("RENT"),
                DeliverymanId = await GetRandomDeliverymanIdAsync(),
                MotorcycleId = await GetRandomMotorcycleIdAsync(),
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(randomPlan),
                ExpectedEndDate = DateTime.UtcNow.Date.AddDays(randomPlan),
                Plan = randomPlan
            };

            var response = await _client.PostAsJsonAsync("/locacao", createRentalDto);
            if ((int)response.StatusCode != 201)
            {
                var ver = createRentalDto;
            }
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task GetRentalById_ReturnsRental()
        {
            await AuthenticateAsync();
            var randomPlan = TestDataGenerator.GetRandomPlan();
            var createRentalDto = new CreateRentalDto
            {
                Identifier = TestDataGenerator.GenerateUniqueIdentifier("RENT"),
                DeliverymanId = await GetRandomDeliverymanIdAsync(),
                MotorcycleId = await GetRandomMotorcycleIdAsync(),
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(randomPlan),
                ExpectedEndDate = DateTime.UtcNow.Date.AddDays(randomPlan),
                Plan = randomPlan
            };

            var createResponse = await _client.PostAsJsonAsync("/locacao", createRentalDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            if ((int)createResponse.StatusCode != 201)
            {
                var ver = createRentalDto;
            }

            var response = await _client.GetAsync($"/locacao/{createRentalDto.Identifier}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var rental = await response.Content.ReadFromJsonAsync<RentalDto>();
            rental.Should().NotBeNull();
            rental.MotorcycleId.Should().Be(createRentalDto.MotorcycleId);
            rental.DeliverymanId.Should().Be(createRentalDto.DeliverymanId);
        }

        [Fact]
        public async Task ReturnRental_ValidId_ReturnsOk()
        {

            await AuthenticateAsync();
            var randomPlan = TestDataGenerator.GetRandomPlan();
            var createRentalDto = new CreateRentalDto
            {
                Identifier = TestDataGenerator.GenerateUniqueIdentifier("RENT"),
                DeliverymanId = await GetRandomDeliverymanIdAsync(),
                MotorcycleId = await GetRandomMotorcycleIdAsync(),
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(randomPlan),
                ExpectedEndDate = DateTime.UtcNow.Date.AddDays(randomPlan),
                Plan = randomPlan
            };

            var createResponse = await _client.PostAsJsonAsync("/locacao", createRentalDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);


            var returnDto = new UpdateReturnDateDto
            {
                ReturnDate = DateTime.UtcNow
            };

            var response = await _client.PutAsJsonAsync($"/locacao/{createRentalDto.Identifier}/devolucao", returnDto);


            response.StatusCode.Should().Be(HttpStatusCode.OK);


            var getResponse = await _client.GetAsync($"/locacao/{createRentalDto.Identifier}");
            var updatedRental = await getResponse.Content.ReadFromJsonAsync<RentalDto>();
            updatedRental.Should().NotBeNull();
            updatedRental.ReturnDate.Should().NotBeNull();
        }
    }
}