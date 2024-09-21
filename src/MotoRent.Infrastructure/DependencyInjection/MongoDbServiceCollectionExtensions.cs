using Microsoft.Extensions.DependencyInjection;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Repositories;

namespace MotoRent.Infrastructure.DependencyInjection
{
    public static class MongoDbServiceCollectionExtensions
    {
        public static IServiceCollection AddRepository(this IServiceCollection services)
        {
            // Register repositories
            services.AddScoped<IMotorcycleRepository, MotorcycleRepository>();
            services.AddScoped<IDeliverymanRepository, DeliverymanRepository>();
            services.AddScoped<IRentalRepository, RentalRepository>();

            return services;
        }
    }
}