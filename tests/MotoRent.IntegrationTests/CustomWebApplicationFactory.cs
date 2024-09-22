using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MotoRent.Application.Mappings;
using MotoRent.Application.Services;
using MotoRent.Infrastructure.Data;
using MotoRent.Infrastructure.Data.Config;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.MessageConsumers.Services;

namespace MotoRent.IntegrationTests
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType ==
                        typeof(IMongoDbContext));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddAutoMapper(typeof(AutoMapperProfile));
                services.AddScoped<IMotorcycleService, MotorcycleService>();
                services.AddScoped<IDeliverymanService, DeliverymanService>();
                services.AddScoped<IRentalService, RentalService>();
                services.AddScoped<IMessageService, RabbitMQService>();

                services.AddScoped<IJwtService, JwtService>();

                services.AddSingleton<IMongoDbContext>(sp =>
                {
                    var client = new MongoClient("mongodb://admin:password@localhost:27017/");
                    var database = client.GetDatabase("TestDb");
                    return new MongoDbContext(new MongoDbConfig { ConnectionString = "mongodb://admin:password@localhost:27017/", DatabaseName = database.DatabaseNamespace.DatabaseName });
                });

                var sp = services.BuildServiceProvider();

                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<IMongoDbContext>();

                }
            });
        }
    }
}