using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MotoRent.Application.Mappings;
using MotoRent.Application.Middlewares;
using MotoRent.Application.Services;
using MotoRent.Infrastructure.Storage;
using MotoRent.MessageConsumers.Services;

namespace MotoRent.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(AutoMapperProfile));
            services.AddScoped<IMotorcycleService, MotorcycleService>();
            services.AddScoped<IDeliverymanService, DeliverymanService>();
            services.AddScoped<IRentalService, RentalService>();

            services.AddSingleton<IFileStorageService, MinioFileStorageService>();

            // Add JWT service
            services.AddScoped<IJwtService, JwtService>();

            services.AddSingleton<IMessageService, RabbitMQService>();

            return services;
        }

        public static void ConfigureCustomExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionMiddleware>();
        }


    }
}