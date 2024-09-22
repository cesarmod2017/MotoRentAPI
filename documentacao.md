### Program.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\Program.cs

Descricao:

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MotoRent.API.BackgroundServices;
using MotoRent.Application.Config;
using MotoRent.Application.DependencyInjection;
using MotoRent.Application.Filters;
using MotoRent.Application.Validators;
using MotoRent.Infrastructure.Data;
using MotoRent.Infrastructure.Data.Config;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.DependencyInjection;
using MotoRent.MessageConsumers.Consumers;
using RabbitMQ.Client;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/motorent-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure JWT
var jwtConfig = builder.Configuration.GetSection("JwtConfig").Get<JwtConfig>();
builder.Services.AddSingleton(jwtConfig);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig.Issuer,
        ValidAudience = jwtConfig.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret))
    };
});

builder.Services.AddAuthorization();


// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidateModelAttribute>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddFluentValidationAutoValidation(config =>
{
    config.DisableDataAnnotationsValidation = true;
});

builder.Services.AddValidatorsFromAssemblyContaining<UpdateLicenseImageDtoValidator>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{


    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = $"MotoRent.Api",
        Version = "v1",

    });
    c.DocInclusionPredicate((_, api) => !string.IsNullOrWhiteSpace(api.GroupName));

    c.TagActionsBy(api => api.GroupName);

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (!File.Exists(xmlPath))
        File.Create(xmlPath);

    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddHostedService<MotorcycleCreatedBackgroundService>();
// Consumer Rabbit
builder.Services.AddScoped<IMotorcycleCreatedConsumer, MotorcycleCreatedConsumer>();

// Add MongoDB services and repositories
builder.Services.Configure<MongoDbConfig>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<IMongoDbContext>(sp =>
{
    var config = sp.GetRequiredService<IOptions<MongoDbConfig>>().Value;
    return new MongoDbContext(config);
});
System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
// Configura��o do RabbitMQ (se necess�rio)
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var config = builder.Configuration.GetSection("RabbitMQ");
    return new ConnectionFactory
    {
        HostName = config["HostName"],
        UserName = config["UserName"],
        Password = config["Password"]
    };
});

builder.Services.AddRepository();

// Add application services
builder.Services.AddApplicationServices();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseSerilogRequestLogging(); // Add this line to log HTTP requests

app.UseRouting(); // Move this up, before authentication and authorization

app.UseAuthentication();
app.UseAuthorization();

app.ConfigureCustomExceptionMiddleware();

app.MapControllers();
try
{
    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}


public partial class Program { }
```

### Class1.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Domain\Class1.cs

Descricao:

```csharp
namespace MotoRent.Domain;

public class Class1
{

}

```

### Class1.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Shared\Class1.cs

Descricao:

```csharp
namespace MotoRent.Shared;

public class Class1
{

}

```

### CustomWebApplicationFactory.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.IntegrationTests\CustomWebApplicationFactory.cs

Descricao:

```csharp
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
```

### MotorcycleCreatedBackgroundService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\BackgroundServices\MotorcycleCreatedBackgroundService.cs

Descricao:

```csharp
using MotoRent.MessageConsumers.Consumers;
using MotoRent.MessageConsumers.Services;

namespace MotoRent.API.BackgroundServices
{
    public class MotorcycleCreatedBackgroundService : BackgroundService
    {
        private readonly IMessageService _messageService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MotorcycleCreatedBackgroundService> _logger;

        public MotorcycleCreatedBackgroundService(
            IMessageService messageService,
            IServiceProvider serviceProvider,
            ILogger<MotorcycleCreatedBackgroundService> logger)
        {
            _messageService = messageService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MotorcycleCreatedBackgroundService is starting.");

            stoppingToken.Register(() =>
                _logger.LogInformation("MotorcycleCreatedBackgroundService is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessMessageAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing message");
                }

                await Task.Delay(1000, stoppingToken); // Aguarda 1 segundo antes de processar a próxima mensagem
            }
        }

        private async Task ProcessMessageAsync(CancellationToken stoppingToken)
        {
            var message = await _messageService.ReceiveAsync("motorcycle-created", stoppingToken);

            if (message != null)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var consumer = scope.ServiceProvider.GetRequiredService<IMotorcycleCreatedConsumer>();
                    await consumer.ConsumeAsync(message);
                }
            }
        }
    }
}

```

### AuthController.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\Controllers\AuthController.cs

Descricao:

```csharp
using Microsoft.AspNetCore.Mvc;
using MotoRent.Application.Services;

namespace MotoRent.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = @"Login")]
    [Route("login")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwtService;

        public AuthController(IJwtService jwtService)
        {
            _jwtService = jwtService;
        }

        /// <summary>
        /// Realizar autenticação
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Login([FromBody] LoginModel model)
        {
            // TODO: Implement actual user authentication
            if (model.Username == "admin" && model.Password == "password")
            {
                var token = _jwtService.GenerateToken("admin-user-id", "Admin");
                return Ok(new { Token = token });
            }
            else if (model.Username.StartsWith("entregador") && model.Password == "password")
            {
                var token = _jwtService.GenerateToken($"{model.Username}-user-id", "Entregador");
                return Ok(new { Token = token });
            }

            return Unauthorized();
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
```

### DeliverymenController.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\Controllers\DeliverymenController.cs

Descricao:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Application.DTOs.Default;
using MotoRent.Application.DTOs.Deliveryman;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.Services;

namespace MotoRent.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = @"Entregadores")]
    [Route("entregadores")]
    public class DeliverymenController : ControllerBase
    {
        private readonly IDeliverymanService _deliverymanService;

        public DeliverymenController(IDeliverymanService deliverymanService)
        {
            _deliverymanService = deliverymanService ?? throw new ArgumentNullException(nameof(deliverymanService));
        }


        /// <summary>
        /// Cadastrar entregador
        /// </summary>
        /// <param name="createDeliverymanDto"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400, Type = typeof(ErrorResponseDto))]
        public async Task<IActionResult> CreateDeliveryman([FromBody] CreateDeliverymanDto createDeliverymanDto)
        {

            var createdDeliveryman = await _deliverymanService.CreateDeliverymanAsync(createDeliverymanDto);
            return StatusCode(StatusCodes.Status201Created);

        }


        /// <summary>
        /// Consultar entregador por id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Entregador")]
        [ProducesResponseType(200, Type = typeof(DeliverymanDto))]
        public async Task<ActionResult<DeliverymanDto>> GetDeliverymanById(string id)
        {

            var deliveryman = await _deliverymanService.GetDeliverymanByIdAsync(id);
            return Ok(deliveryman);

        }


        /// <summary>
        /// Enviar foto da CNH
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updateLicenseImageDto"></param>
        /// <returns></returns>
        [HttpPost("{id}/cnh")]
        [Authorize(Roles = "Entregador")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400, Type = typeof(ErrorResponseDto))]
        public async Task<IActionResult> UpdateLicenseImage(string id, [FromBody] UpdateLicenseImageDto updateLicenseImageDto)
        {

            await _deliverymanService.UpdateLicenseImageAsync(id, updateLicenseImageDto);
            return StatusCode(StatusCodes.Status201Created);

        }
    }
}
```

### MotorcyclesController.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\Controllers\MotorcyclesController.cs

Descricao:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Application.DTOs.Default;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.Services;

namespace MotoRent.API.Controllers
{
    [Authorize]
    [ApiController]
    [ApiExplorerSettings(GroupName = @"Motos")]
    [Route("motos")]
    public class MotorcyclesController : ControllerBase
    {
        private readonly IMotorcycleService _motorcycleService;

        public MotorcyclesController(IMotorcycleService motorcycleService)
        {
            _motorcycleService = motorcycleService ?? throw new ArgumentNullException(nameof(motorcycleService));
        }

        /// <summary>
        /// Consultar motos existentes
        /// </summary>
        /// <param name="licensePlate"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<MotorcycleDto>))]
        public async Task<ActionResult<IEnumerable<MotorcycleDto>>> GetAllMotorcycles([FromQuery] string? placa)
        {
            if (!string.IsNullOrEmpty(placa))
            {
                var motorcyclesByPlate = await _motorcycleService.GetMotorcyclesByLicensePlateAsync(placa);
                return Ok(motorcyclesByPlate);
            }

            var motorcycles = await _motorcycleService.GetAllMotorcyclesAsync();
            return Ok(motorcycles);
        }

        /// <summary>
        /// Consultar motos exisntes por id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(200, Type = typeof(MotorcycleDto))]
        [ProducesResponseType(400, Type = typeof(ErrorResponseDto))]
        [ProducesResponseType(404, Type = typeof(ErrorResponseDto))]
        public async Task<ActionResult<MotorcycleDto>> GetMotorcycleById(string id)
        {
            var motorcycle = await _motorcycleService.GetMotorcycleByIdAsync(id);
            return Ok(motorcycle);
        }


        /// <summary>
        /// Cadastrar uma nova moto
        /// </summary>
        /// <param name="createMotorcycleDto"></param>
        /// <returns></returns>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400, Type = typeof(ErrorResponseDto))]
        public async Task<ActionResult> CreateMotorcycle([FromBody] CreateMotorcycleDto createMotorcycleDto)
        {
            var createdMotorcycle = await _motorcycleService.CreateMotorcycleAsync(createMotorcycleDto);
            return StatusCode(StatusCodes.Status201Created);

        }


        /// <summary>
        /// Modificar a placa de uma moto
        /// </summary>
        /// <param name="id"></param>
        /// <param name="placa"></param>
        /// <returns></returns>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/placa")]
        [ProducesResponseType(200, Type = typeof(SuccessMotorcycleResponseDto))]
        [ProducesResponseType(400, Type = typeof(ErrorResponseDto))]
        public async Task<IActionResult> UpdateMotorcycleLicensePlate(string id, [FromBody] UpdateLicensePlateDto updateLicensePlateDto)
        {

            await _motorcycleService.UpdateMotorcycleLicensePlateAsync(id, updateLicensePlateDto);
            return Ok(new SuccessMotorcycleResponseDto { Message = "License plate modified successfully" });

        }

        /// <summary>
        /// Remover uma moto
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400, Type = typeof(ErrorResponseDto))]
        public async Task<IActionResult> DeleteMotorcycle(string id)
        {

            await _motorcycleService.DeleteMotorcycleAsync(id);
            return Ok();

        }
    }
}
```

### RentalsController.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\Controllers\RentalsController.cs

Descricao:

```csharp

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Application.DTOs.Default;
using MotoRent.Application.DTOs.Rental;
using MotoRent.Application.Services;

namespace MotoRent.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = @"Locação")]
    [Route("locacao")]
    public class RentalsController : ControllerBase
    {
        private readonly IRentalService _rentalService;

        public RentalsController(IRentalService rentalService)
        {
            _rentalService = rentalService ?? throw new ArgumentNullException(nameof(rentalService));
        }


        /// <summary>
        /// Alugar uma moto 
        /// </summary>
        /// <param name="createRentalDto"></param>
        /// <returns></returns>
        [Authorize(Roles = "Admin,Entregador")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400, Type = typeof(ErrorResponseDto))]
        [HttpPost]
        public async Task<IActionResult> CreateRental([FromBody] CreateRentalDto createRentalDto)
        {

            var createdRental = await _rentalService.CreateRentalAsync(createRentalDto);
            return StatusCode(StatusCodes.Status201Created);

        }

        /// <summary>
        /// Consultar locação por id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Admin,Entregador")]
        [ProducesResponseType(200, Type = typeof(RentalDto))]
        [ProducesResponseType(400, Type = typeof(ErrorResponseDto))]
        [ProducesResponseType(404, Type = typeof(ErrorResponseDto))]
        [HttpGet("{id}")]
        public async Task<ActionResult<RentalDto>> GetRentalById(string id)
        {
            var rental = await _rentalService.GetRentalByIdAsync(id);
            return Ok(rental);

        }


        /// <summary>
        /// Informar a data de devolução e calcular valor
        /// </summary>
        /// <param name="id"></param>
        /// <param name="returnDate"></param>
        /// <returns></returns>
        [Authorize(Roles = "Admin,Entregador")]
        [ProducesResponseType(200, Type = typeof(RentalCalculationResultDto))]
        [ProducesResponseType(400, Type = typeof(ErrorResponseDto))]
        [HttpPut("{id}/devolucao")]
        public async Task<ActionResult<RentalCalculationResultDto>> CalculateRentalCost(string id, [FromBody] UpdateReturnDateDto returnDateDto)
        {

            var result = await _rentalService.CalculateRentalCostAsync(id, returnDateDto);
            return Ok(result);

        }
    }
}
```

### HttpResponseExceptionFilter.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\Filters\HttpResponseExceptionFilter.cs

Descricao:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MotoRent.API.Filters
{
    public class HttpResponseExceptionFilter : IActionFilter, IOrderedFilter
    {
        public int Order { get; set; } = int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception is ArgumentException argumentException)
            {
                context.Result = new ObjectResult(new { message = argumentException.Message })
                {
                    StatusCode = 400
                };
                context.ExceptionHandled = true;
            }
            else if (context.Exception is Exception)
            {
                context.Result = new ObjectResult(new { message = "An error occurred" })
                {
                    StatusCode = 500
                };
                context.ExceptionHandled = true;
            }
        }
    }
}
```

### JwtConfig.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Config\JwtConfig.cs

Descricao:

```csharp
namespace MotoRent.Application.Config
{
    public class JwtConfig
    {
        public string Secret { get; set; }
        public int ExpirationInMinutes { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
    }
}
```

### ApplicationServiceCollectionExtensions.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DependencyInjection\ApplicationServiceCollectionExtensions.cs

Descricao:

```csharp
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
```

### ValidateModelAttribute.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Filters\ValidateModelAttribute.cs

Descricao:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MotoRent.Application.DTOs.Default;

namespace MotoRent.Application.Filters
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToArray();

                var errorResponse = new ErrorResponseDto
                {
                    Message = string.Join(" ", errors)
                };

                context.Result = new BadRequestObjectResult(errorResponse);
            }
        }
    }
}

```

### CnpjGenerator.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Helpers\CnpjGenerator.cs

Descricao:

```csharp
namespace MotoRent.Application.Helpers
{
    public class CnpjGenerator
    {
        public static string GenerateCnpj()
        {
            Random random = new Random();
            int[] cnpj = new int[14];

            // Gera os 12 primeiros dígitos
            for (int i = 0; i < 12; i++)
            {
                cnpj[i] = random.Next(0, 10);
            }

            // Calcula o primeiro dígito verificador
            cnpj[12] = CalculateCnpjDigit(cnpj, new int[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });

            // Calcula o segundo dígito verificador
            cnpj[13] = CalculateCnpjDigit(cnpj, new int[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 });

            // Formata o CNPJ em string no formato 00.000.000/0000-00
            return $"{cnpj[0]}{cnpj[1]}{cnpj[2]}{cnpj[3]}{cnpj[4]}{cnpj[5]}{cnpj[6]}{cnpj[7]}{cnpj[8]}{cnpj[9]}{cnpj[10]}{cnpj[11]}{cnpj[12]}{cnpj[13]}";
        }

        private static int CalculateCnpjDigit(int[] cnpj, int[] weights)
        {
            int sum = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                sum += cnpj[i] * weights[i];
            }

            int remainder = sum % 11;
            return remainder < 2 ? 0 : 11 - remainder;
        }



    }
}

```

### DeliverymanIdentifierGenerator.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Helpers\DeliverymanIdentifierGenerator.cs

Descricao:

```csharp
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace MotoRent.Application.Helpers
{
    public class DeliverymanIdentifierGenerator
    {
        private const string Prefix = "DEL";
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int IdLength = 6; // 3 for prefix + 3 for unique identifier
        private static readonly ConcurrentDictionary<string, byte> UsedIds = new ConcurrentDictionary<string, byte>();

        public static string GenerateUniqueIdentifier()
        {
            string id;
            do
            {
                id = GenerateId();
            } while (!UsedIds.TryAdd(id, 0));

            return id;
        }

        private static string GenerateId()
        {
            var result = new StringBuilder(Prefix);

            for (int i = Prefix.Length; i < IdLength; i++)
            {
                var randomIndex = RandomNumberGenerator.GetInt32(0, Characters.Length);
                result.Append(Characters[randomIndex]);
            }

            return result.ToString();
        }

        // Optional: Method to reset used IDs (for testing purposes)
        public static void ResetUsedIds()
        {
            UsedIds.Clear();
        }
    }
}

```

### ImageHelper.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Helpers\ImageHelper.cs

Descricao:

```csharp
namespace MotoRent.Application.Helpers
{
    public class ImageHelper
    {
        public static string ConvertImageToBase64(string relativePath)
        {
            // Caminho do projeto (trabalhando com o diretorio pai do bin durante a execucao do teste)
            var projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

            // Combina o caminho do projeto com o caminho relativo da imagem
            var absolutePath = Path.Combine(projectDirectory, relativePath);

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Arquivo de imagem não encontrado no caminho especificado", absolutePath);
            }

            // Ler o arquivo como bytes
            byte[] imageBytes = File.ReadAllBytes(absolutePath);

            // Converte os bytes da imagem em uma string base64
            string base64String = Convert.ToBase64String(imageBytes);

            return base64String;
        }
    }


}

```

### LicenseNumberGenerator.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Helpers\LicenseNumberGenerator.cs

Descricao:

```csharp
namespace MotoRent.Application.Helpers
{
    public class LicenseNumberGenerator
    {
        public static string GenerateLicenseNumber()
        {
            Random random = new Random();
            string licenseNumber = string.Empty;

            // Gera 11 dígitos aleatórios
            for (int i = 0; i < 11; i++)
            {
                licenseNumber += random.Next(0, 10).ToString();
            }

            return licenseNumber;
        }


    }

}

```

### MotorcycleDtoGenerator.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Helpers\MotorcycleDtoGenerator.cs

Descricao:

```csharp
using MotoRent.Application.DTOs.Motorcycle;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace MotoRent.Application.Helpers
{
    public static class MotorcycleDtoGenerator
    {
        private static readonly ConcurrentDictionary<string, byte> UsedIdentifiers = new ConcurrentDictionary<string, byte>();
        private static readonly ConcurrentDictionary<string, byte> UsedLicensePlates = new ConcurrentDictionary<string, byte>();
        private static int LastIdentifierNumber = 0;

        private static readonly List<string> MotorcycleModels = new List<string>
        {
            "Honda CG 160", "Yamaha Factor 150", "Honda Biz 125", "Yamaha MT-03", "Honda CB 500F",
            "Kawasaki Ninja 400", "Suzuki GSX-S750", "BMW G 310 R", "Harley-Davidson Iron 883",
            "Triumph Street Triple", "Ducati Monster", "Honda XRE 300", "Yamaha Fazer 250", "Honda CB 250F Twister"
        };

        private static readonly string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static CreateMotorcycleDto Generate()
        {
            return new CreateMotorcycleDto
            {
                Identifier = GenerateUniqueIdentifier(),
                Year = GenerateRandomYear(),
                Model = GetRandomModel(),
                LicensePlate = GenerateUniqueLicensePlate()
            };
        }

        private static string GenerateUniqueIdentifier()
        {
            string identifier;
            do
            {
                StringBuilder sb = new StringBuilder("MOTO");
                for (int i = 0; i < 5; i++)
                {
                    sb.Append(RandomNumberGenerator.GetInt32(0, 10));
                }
                identifier = sb.ToString();
            } while (!UsedIdentifiers.TryAdd(identifier, 0));

            return identifier;
        }
        private static int GenerateRandomYear()
        {
            return RandomNumberGenerator.GetInt32(2020, 2025); // 2025 because GetInt32 upper bound is exclusive
        }

        private static string GetRandomModel()
        {
            int index = RandomNumberGenerator.GetInt32(0, MotorcycleModels.Count);
            return MotorcycleModels[index];
        }

        private static string GenerateUniqueLicensePlate()
        {
            string licensePlate;
            do
            {
                StringBuilder plate = new StringBuilder();
                for (int i = 0; i < 3; i++)
                {
                    plate.Append(Letters[RandomNumberGenerator.GetInt32(0, Letters.Length)]);
                }
                plate.Append('-');
                for (int i = 0; i < 4; i++)
                {
                    plate.Append(RandomNumberGenerator.GetInt32(0, 10));
                }
                licensePlate = plate.ToString();
            } while (!UsedLicensePlates.TryAdd(licensePlate, 0));

            return licensePlate;
        }

        public static void ResetUsedIdentifiersAndPlates()
        {
            UsedIdentifiers.Clear();
            UsedLicensePlates.Clear();
            LastIdentifierNumber = 0;
        }
    }
}
```

### NameGenerator.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Helpers\NameGenerator.cs

Descricao:

```csharp
using System.Security.Cryptography;

namespace MotoRent.Application.Helpers
{
    public static class NameGenerator
    {
        private static readonly List<string> FirstNames = new List<string>
        {
            "João", "Maria", "Pedro", "Ana", "Carlos", "Mariana", "José", "Fernanda", "Paulo", "Beatriz",
            "Lucas", "Juliana", "André", "Camila", "Felipe", "Gabriela", "Ricardo", "Isabela", "Daniel", "Larissa"
        };

        private static readonly List<string> LastNames = new List<string>
        {
            "Silva", "Santos", "Oliveira", "Souza", "Rodrigues", "Ferreira", "Alves", "Pereira", "Lima", "Gomes",
            "Costa", "Ribeiro", "Martins", "Carvalho", "Almeida", "Lopes", "Soares", "Fernandes", "Vieira", "Barbosa"
        };

        public static string GenerateFullName()
        {
            string firstName = GetRandomElement(FirstNames);
            string lastName = GetRandomElement(LastNames);

            return $"{firstName} {lastName}";
        }

        private static string GetRandomElement(List<string> list)
        {
            int index = RandomNumberGenerator.GetInt32(0, list.Count);
            return list[index];
        }

        public static void AddFirstName(string firstName)
        {
            if (!string.IsNullOrWhiteSpace(firstName) && !FirstNames.Contains(firstName))
            {
                FirstNames.Add(firstName);
            }
        }

        public static void AddLastName(string lastName)
        {
            if (!string.IsNullOrWhiteSpace(lastName) && !LastNames.Contains(lastName))
            {
                LastNames.Add(lastName);
            }
        }
    }
}

```

### TestDataGenerator.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Helpers\TestDataGenerator.cs

Descricao:

```csharp
using System.Security.Cryptography;

namespace MotoRent.Application.Helpers
{
    public static class TestDataGenerator
    {
        private static readonly List<int> AllowedPlans = new List<int> { 7, 15, 30, 45, 50 };

        public static int GetRandomPlan()
        {
            int index = RandomNumberGenerator.GetInt32(0, AllowedPlans.Count);
            return AllowedPlans[index];
        }

        public static string GenerateUniqueIdentifier(string prefix)
        {
            return $"{prefix}{DateTime.Now:yyyyMMddHHmmssfff}";
        }

    }
}

```

### AutoMapperProfile.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Mappings\AutoMapperProfile.cs

Descricao:

```csharp
using AutoMapper;
using MotoRent.Application.DTOs.Deliveryman;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.DTOs.Rental;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<MotorcycleModel, MotorcycleDto>().ReverseMap();
            CreateMap<CreateMotorcycleDto, MotorcycleModel>();

            CreateMap<DeliverymanModel, DeliverymanDto>().ReverseMap();
            CreateMap<CreateDeliverymanDto, DeliverymanModel>();

            CreateMap<RentalModel, RentalDto>().ReverseMap();
            CreateMap<CreateRentalDto, RentalModel>();
        }
    }
}
```

### ExceptionMiddleware.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Middlewares\ExceptionMiddleware.cs

Descricao:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MotoRent.Application.DTOs.Default;
using MotoRent.Infrastructure.Exceptions;
using MotoRent.MessageConsumers.Services;
using System.Net;
using System.Text.Json;

namespace MotoRent.Application.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IMessageService _messageService;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IMessageService messageService)
        {
            _next = next;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                try
                {
                    await _next(context);

                    if (context.Response.StatusCode == 400 && context.Response.ContentType != null && context.Response.ContentType.Contains("application/problem+json"))
                    {
                        await HandleValidationErrorResponse(context);
                    }
                }
                catch (Exception ex)
                {
                    await HandleExceptionAsync(context, ex);
                }
                finally
                {
                    context.Response.Body.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
        }

        private async Task HandleValidationErrorResponse(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var problemDetailsJson = await reader.ReadToEndAsync();

            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(problemDetailsJson);

            if (problemDetails?.Extensions != null &&
                problemDetails.Extensions.TryGetValue("errors", out var errorsObj) &&
                errorsObj is JsonElement errorsElement)
            {
                var errors = new List<string>();
                foreach (var errorProperty in errorsElement.EnumerateObject())
                {
                    if (errorProperty.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var errorMessage in errorProperty.Value.EnumerateArray())
                        {
                            errors.Add(errorMessage.GetString());
                        }
                    }
                }

                var errorResponse = new ErrorResponseDto
                {
                    Message = string.Join(" ", errors)
                };

                context.Response.Body.SetLength(0);
                await WriteErrorResponse(context, errorResponse, HttpStatusCode.BadRequest);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var errorResponse = new ErrorResponseDto
            {
                Message = "An error occurred while processing your request."
            };

            var statusCode = HttpStatusCode.InternalServerError;

            switch (exception)
            {
                case ValidationException validationException:
                    errorResponse.Message = string.Join(" ", validationException.Errors.Select(e => e.ErrorMessage));
                    statusCode = HttpStatusCode.BadRequest;
                    break;
                case ArgumentException:
                    errorResponse.Message = exception.Message;
                    statusCode = HttpStatusCode.BadRequest;
                    break;
                case NotFoundException:
                    errorResponse.Message = exception.Message;
                    statusCode = HttpStatusCode.NotFound;
                    break;
                default:
                    _logger.LogError(exception, "Unhandled exception occurred");
                    break;
            }

            await WriteErrorResponse(context, errorResponse, statusCode);

            // Log the error to RabbitMQ
            await _messageService.PublishErrorLogAsync($"Error: {errorResponse.Message}");
        }

        private async Task WriteErrorResponse(HttpContext context, ErrorResponseDto errorResponse, HttpStatusCode statusCode)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var result = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(result);
        }
    }


}

```

### DeliverymanService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Services\DeliverymanService.cs

Descricao:

```csharp
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

```

### IDeliverymanService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Services\IDeliverymanService.cs

Descricao:

```csharp
using MotoRent.Application.DTOs.Deliveryman;
using MotoRent.Application.DTOs.Motorcycle;

namespace MotoRent.Application.Services
{
    public interface IDeliverymanService
    {
        Task<DeliverymanDto> CreateDeliverymanAsync(CreateDeliverymanDto createDeliverymanDto);
        Task<DeliverymanDto> GetDeliverymanByIdAsync(string id);
        Task<string> GetRandomDeliverymanIdAsync();
        Task UpdateLicenseImageAsync(string id, UpdateLicenseImageDto updateLicenseImageDto);
    }
}
```

### IJwtService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Services\IJwtService.cs

Descricao:

```csharp
namespace MotoRent.Application.Services
{
    public interface IJwtService
    {
        string GenerateToken(string userId, string role);
    }
}
```

### IMotorcycleService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Services\IMotorcycleService.cs

Descricao:

```csharp
using MotoRent.Application.DTOs.Motorcycle;

namespace MotoRent.Application.Services
{
    public interface IMotorcycleService
    {
        Task<IEnumerable<MotorcycleDto>> GetAllMotorcyclesAsync();
        Task<MotorcycleDto> GetMotorcycleByIdAsync(string id);
        Task<MotorcycleDto> CreateMotorcycleAsync(CreateMotorcycleDto createMotorcycleDto);
        Task UpdateMotorcycleLicensePlateAsync(string id, UpdateLicensePlateDto updateLicensePlateDto);
        Task DeleteMotorcycleAsync(string id);
        Task<IEnumerable<MotorcycleDto>> GetMotorcyclesByLicensePlateAsync(string licensePlate);
        Task<string> GetRandomMotorcycleIdAsync();
    }
}
```

### IRentalService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Services\IRentalService.cs

Descricao:

```csharp
using MotoRent.Application.DTOs.Rental;

namespace MotoRent.Application.Services
{
    public interface IRentalService
    {
        Task<RentalDto> CreateRentalAsync(CreateRentalDto createRentalDto);
        Task<RentalDto> GetRentalByIdAsync(string id);
        Task<RentalCalculationResultDto> CalculateRentalCostAsync(string id, UpdateReturnDateDto returnDateDto);
    }
}
```

### JwtService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Services\JwtService.cs

Descricao:

```csharp
using Microsoft.IdentityModel.Tokens;
using MotoRent.Application.Config;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MotoRent.Application.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtConfig _jwtConfig;

        public JwtService(JwtConfig jwtConfig)
        {
            _jwtConfig = jwtConfig;
        }

        public string GenerateToken(string userId, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtConfig.ExpirationInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
```

### MotorcycleService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Services\MotorcycleService.cs

Descricao:

```csharp
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;
using MotoRent.Infrastructure.Exceptions;
using MotoRent.MessageConsumers.Events;
using MotoRent.MessageConsumers.Services;

namespace MotoRent.Application.Services
{
    public class MotorcycleService : IMotorcycleService
    {
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateMotorcycleDto> _createMotorcycleValidator;
        private readonly ILogger<MotorcycleService> _logger;
        private readonly IMessageService _messageService;
        private readonly IRentalRepository _rentalRepository;

        public MotorcycleService(
            IMotorcycleRepository motorcycleRepository,
            IMapper mapper,
            IValidator<CreateMotorcycleDto> createMotorcycleValidator,
            ILogger<MotorcycleService> logger,
            IMessageService messageService,
            IRentalRepository rentalRepository)
        {
            _motorcycleRepository = motorcycleRepository ?? throw new ArgumentNullException(nameof(motorcycleRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createMotorcycleValidator = createMotorcycleValidator ?? throw new ArgumentNullException(nameof(createMotorcycleValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageService = messageService;
            _rentalRepository = rentalRepository;
        }

        public async Task<IEnumerable<MotorcycleDto>> GetAllMotorcyclesAsync()
        {
            _logger.LogInformation("Buscando todas as motos");
            var motorcycles = await _motorcycleRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<MotorcycleDto>>(motorcycles);
        }

        public async Task<MotorcycleDto> GetMotorcycleByIdAsync(string id)
        {
            _logger.LogInformation("Buscando moto com ID: {MotorcycleId}", id);
            var motorcycle = await _motorcycleRepository.GetByFieldStringAsync("identifier", id);
            if (motorcycle == null)
            {
                _logger.LogWarning("Moto não encontrada com ID: {MotorcycleId}", id);
                throw new NotFoundException($"Moto não encontrada: {id}");
            }
            return _mapper.Map<MotorcycleDto>(motorcycle);
        }

        public async Task<MotorcycleDto> CreateMotorcycleAsync(CreateMotorcycleDto createMotorcycleDto)
        {
            _logger.LogInformation("Criando nova moto: {@CreateMotorcycleDto}", createMotorcycleDto);

            var validationResult = await _createMotorcycleValidator.ValidateAsync(createMotorcycleDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Falha na validação para a solicitação de criação de moto: {@ValidationErrors}", validationResult.Errors);
                throw new ValidationException(validationResult.Errors);
            }

            var existingMotorcycle = await _motorcycleRepository.GetByLicensePlateAsync(createMotorcycleDto.LicensePlate);
            if (existingMotorcycle != null && existingMotorcycle.Any())
            {
                throw new ArgumentException("Já existe uma moto cadastrada com esta placa. Por favor, verifique e tente novamente com uma placa diferente.");
            }

            var existingMotorcycleByIdentifier = await _motorcycleRepository.GetByFieldStringAsync("identifier", createMotorcycleDto.Identifier);
            if (existingMotorcycleByIdentifier != null)
            {
                throw new ArgumentException("Já existe uma moto cadastrada com este identificador. Por favor, use um identificador único.");
            }

            var motorcycle = _mapper.Map<MotorcycleModel>(createMotorcycleDto);
            await _motorcycleRepository.CreateAsync(motorcycle);

            _logger.LogInformation("Moto criada com sucesso: {@Motorcycle}", motorcycle);

            await PublishMotorcycleCreatedEventAsync(motorcycle);

            return _mapper.Map<MotorcycleDto>(motorcycle);
        }

        private async Task PublishMotorcycleCreatedEventAsync(MotorcycleModel motorcycle)
        {
            var motorcycleCreatedEvent = new MotorcycleCreatedEvent
            {
                Id = motorcycle.Id,
                Identifier = motorcycle.Identifier,
                Year = motorcycle.Year,
                Model = motorcycle.Model,
                LicensePlate = motorcycle.LicensePlate
            };
            await _messageService.PublishAsync("motorcycle-created", motorcycleCreatedEvent);
        }

        public async Task UpdateMotorcycleLicensePlateAsync(string id, UpdateLicensePlateDto updateLicensePlateDto)
        {
            _logger.LogInformation("Atualizando placa da moto com ID: {MotorcycleId}", id);
            var motorcycle = await _motorcycleRepository.GetByFieldStringAsync("identifier", id);
            if (motorcycle == null)
            {
                _logger.LogWarning("Moto não encontrada com ID: {MotorcycleId}", id);
                throw new ArgumentException("Moto não encontrada", nameof(id));
            }

            motorcycle.LicensePlate = updateLicensePlateDto.LicensePlate;
            await _motorcycleRepository.UpdateAsync(motorcycle.Id, motorcycle);
            _logger.LogInformation("Placa atualizada com sucesso para a moto com ID: {MotorcycleId}", id);
        }

        public async Task DeleteMotorcycleAsync(string id)
        {
            var motorcycle = await _motorcycleRepository.GetByFieldStringAsync("identifier", id);
            if (motorcycle == null)
            {
                _logger.LogWarning("Moto não encontrada com ID: {MotorcycleId}", id);
                throw new NotFoundException($"Moto não encontrada: {id}");
            }

            var rentalExists = await _rentalRepository.ExistsForMotorcycleAsync(id);
            if (rentalExists)
            {
                throw new InvalidOperationException("Não é possível remover a moto pois existem locações associadas a ela.");
            }

            _logger.LogInformation("Excluindo moto com ID: {MotorcycleId}", id);

            await _motorcycleRepository.DeleteAsync(motorcycle.Id);

            _logger.LogInformation("Moto excluída com sucesso com ID: {MotorcycleId}", id);
        }

        public async Task<IEnumerable<MotorcycleDto>> GetMotorcyclesByLicensePlateAsync(string licensePlate)
        {
            _logger.LogInformation("Buscando motos com placa: {LicensePlate}", licensePlate);
            var motorcycles = await _motorcycleRepository.GetByLicensePlateAsync(licensePlate);
            return _mapper.Map<IEnumerable<MotorcycleDto>>(motorcycles);
        }

        public async Task<string> GetRandomMotorcycleIdAsync()
        {
            var motorcycles = await _motorcycleRepository.GetAllAsync();
            var randomMotorcycle = motorcycles.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
            return randomMotorcycle?.Identifier;
        }
    }
}

```

### RentalService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Services\RentalService.cs

Descricao:

```csharp
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MotoRent.Application.DTOs.Rental;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Application.Services
{
    public class RentalService : IRentalService
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly IDeliverymanRepository _deliverymanRepository;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateRentalDto> _createRentalValidator;
        private readonly ILogger<RentalService> _logger;

        public RentalService(
            IRentalRepository rentalRepository,
            IDeliverymanRepository deliverymanRepository,
            IMotorcycleRepository motorcycleRepository,
            IMapper mapper,
            IValidator<CreateRentalDto> createRentalValidator,
            ILogger<RentalService> logger)
        {
            _rentalRepository = rentalRepository ?? throw new ArgumentNullException(nameof(rentalRepository));
            _deliverymanRepository = deliverymanRepository ?? throw new ArgumentNullException(nameof(deliverymanRepository));
            _motorcycleRepository = motorcycleRepository ?? throw new ArgumentNullException(nameof(motorcycleRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _createRentalValidator = createRentalValidator ?? throw new ArgumentNullException(nameof(createRentalValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RentalDto> CreateRentalAsync(CreateRentalDto createRentalDto)
        {
            _logger.LogInformation("Criando nova locação: {@CreateRentalDto}", createRentalDto);

            var validationResult = await _createRentalValidator.ValidateAsync(createRentalDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Falha na validação para a solicitação de criação de locação: {@ValidationErrors}", validationResult.Errors);
                throw new ValidationException(validationResult.Errors);
            }

            var rentalExist = await _rentalRepository.GetByIdentifierAsync(createRentalDto.Identifier);
            if (rentalExist != null)
            {
                _logger.LogWarning("Este identificador já existe para outra locação: {Identifier}", createRentalDto.Identifier);
                throw new ArgumentException($"Este identificador já existe para outra locação: {createRentalDto.Identifier}");
            }

            var deliveryman = await _deliverymanRepository.GetByIdentifierAsync(createRentalDto.DeliverymanId);
            if (deliveryman == null)
            {
                _logger.LogWarning("Entregador não encontrado com o ID: {DeliverymanId}", createRentalDto.DeliverymanId);
                throw new ArgumentException("Entregador não encontrado");
            }

            if (deliveryman.LicenseType != "A" && deliveryman.LicenseType != "AB")
            {
                _logger.LogWarning("O entregador {DeliverymanId} não possui o tipo de licença necessário", createRentalDto.DeliverymanId);
                throw new ArgumentException("O entregador deve ter licença do tipo A ou AB");
            }

            var motorcycle = await _motorcycleRepository.GetByIdentifierAsync(createRentalDto.MotorcycleId);
            if (motorcycle == null)
            {
                _logger.LogWarning("Motocicleta não encontrada com o ID: {MotorcycleId}", createRentalDto.MotorcycleId);
                throw new ArgumentException("Motocicleta não encontrada");
            }

            var rental = _mapper.Map<RentalModel>(createRentalDto);
            rental.DailyRate = CalculateDailyRate(createRentalDto.Plan);
            rental.StartDate = DateTime.UtcNow.Date.AddDays(1);
            rental.EndDate = rental.StartDate.AddDays(createRentalDto.Plan);
            rental.ExpectedEndDate = rental.EndDate;

            await _rentalRepository.CreateAsync(rental);

            _logger.LogInformation("Locação criada com sucesso: {@Rental}", rental);

            return _mapper.Map<RentalDto>(rental);
        }

        public async Task<RentalDto> GetRentalByIdAsync(string id)
        {
            _logger.LogInformation("Buscando locação com ID: {RentalId}", id);
            var rental = await _rentalRepository.GetByIdentifierAsync(id);
            if (rental == null)
            {
                _logger.LogWarning("Locação não encontrada com ID: {RentalId}", id);
                throw new ArgumentException("Locação não encontrada", nameof(id));
            }
            return _mapper.Map<RentalDto>(rental);
        }

        public async Task<RentalCalculationResultDto> CalculateRentalCostAsync(string id, UpdateReturnDateDto returnDateDto)
        {
            _logger.LogInformation("Calculando custo da locação para o ID: {RentalId} com data de retorno: {ReturnDate}", id, returnDateDto.ReturnDate);

            var rental = await _rentalRepository.GetByIdentifierAsync(id);
            if (rental == null)
            {
                _logger.LogWarning("Locação não encontrada com ID: {RentalId}", id);
                throw new ArgumentException("Locação não encontrada", nameof(id));
            }

            var totalDays = (int)(returnDateDto.ReturnDate - rental.StartDate).TotalDays;
            var plannedDays = (int)(rental.ExpectedEndDate - rental.StartDate).TotalDays;

            decimal totalCost = 0;
            string message = "";

            if (totalDays <= plannedDays)
            {
                totalCost = totalDays * rental.DailyRate;
                if (totalDays < plannedDays)
                {
                    var unusedDays = plannedDays - totalDays;
                    var penaltyRate = GetPenaltyRate(rental.Plan);
                    var penaltyCost = unusedDays * rental.DailyRate * penaltyRate;
                    var rentalCost = totalDays * rental.DailyRate;
                    totalCost = rentalCost + penaltyCost;
                    message = $"Devolução antecipada. Valor da diária: {rentalCost:C}. Multa aplicada: {penaltyCost:C}. Valor total a ser pago: {totalCost:C}";
                    _logger.LogInformation("Devolução antecipada para locação {RentalId}. Multa aplicada: {PenaltyCost}", id, penaltyCost);
                }
                else
                {
                    message = $"Devolução no prazo. Valor total a ser pago: {totalCost:C}";
                }
            }
            else
            {
                var extraDays = totalDays - plannedDays;
                var regularCost = plannedDays * rental.DailyRate;
                var extraCost = extraDays * 50;
                totalCost = regularCost + extraCost;
                message = $"Devolução atrasada. Valor da diária: {regularCost:C}. Cobrança extra por {extraDays} dias: {extraCost:C}. Valor total a ser pago: {totalCost:C}";
                _logger.LogInformation("Devolução atrasada para locação {RentalId}. Cobrança extra: {ExtraCharge}", id, extraCost);
            }

            await _rentalRepository.UpdateReturnDateAsync(id, returnDateDto.ReturnDate, totalCost);

            _logger.LogInformation("Custo da locação calculado para locação {RentalId}. Custo total: {TotalCost}", id, totalCost);

            return new RentalCalculationResultDto
            {
                TotalCost = totalCost,
                Message = message
            };
        }

        private decimal CalculateDailyRate(int plan)
        {
            return plan switch
            {
                7 => 30.0m,
                15 => 28.0m,
                30 => 22.0m,
                45 => 20.0m,
                50 => 18.0m,
                _ => throw new ArgumentException("Plano inválido")
            };
        }

        private decimal GetPenaltyRate(int plan)
        {
            return plan switch
            {
                7 => 0.20m,
                15 => 0.40m,
                _ => 0
            };
        }
    }
}

```

### CreateDeliverymanDtoValidator.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Validators\CreateDeliverymanDtoValidator.cs

Descricao:

```csharp
using FluentValidation;
using MotoRent.Application.DTOs.Deliveryman;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using System.Text.RegularExpressions;

namespace MotoRent.Application.Validators
{
    public class CreateDeliverymanDtoValidator : AbstractValidator<CreateDeliverymanDto>
    {
        public CreateDeliverymanDtoValidator()
        {
            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("O identificador é obrigatório.")
                .MaximumLength(50).WithMessage("O identificador não deve exceder 50 caracteres.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não deve exceder 100 caracteres.");

            RuleFor(x => x.CNPJ)
                .NotEmpty().WithMessage("O CNPJ é obrigatório.")
                .Must(BeValidCNPJ).WithMessage("O CNPJ fornecido não é válido.");

            RuleFor(x => x.BirthDate)
                .NotEmpty().WithMessage("A data de nascimento é obrigatória.")
                .LessThan(DateTime.Now.AddYears(-18)).WithMessage("O entregador deve ter pelo menos 18 anos de idade.");

            RuleFor(x => x.LicenseNumber)
                .NotEmpty().WithMessage("O número da licença é obrigatório.")
                .Must(BeValidLicenseNumber).WithMessage("O número da licença fornecido não é válido.");

            RuleFor(x => x.LicenseType)
                .NotEmpty().WithMessage("O tipo de licença é obrigatório.")
                .Must(x => x == "A" || x == "B" || x == "AB")
                .WithMessage("O tipo de licença deve ser A, B ou AB.");

            RuleFor(x => x.LicenseImage)
                .NotEmpty().WithMessage("A imagem da licença é obrigatória.")
                .Must(BeAValidImage).WithMessage("A imagem da licença deve estar em formato PNG ou BMP.");
        }

        private bool BeValidCNPJ(string cnpj)
        {
            cnpj = new string(cnpj.Where(char.IsDigit).ToArray());

            if (cnpj.Length != 14)
                return false;

            int[] multiplier1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplier2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCnpj = cnpj.Substring(0, 12);
            int sum = 0;

            for (int i = 0; i < 12; i++)
                sum += int.Parse(tempCnpj[i].ToString()) * multiplier1[i];

            int remainder = (sum % 11);
            if (remainder < 2)
                remainder = 0;
            else
                remainder = 11 - remainder;

            string digit = remainder.ToString();
            tempCnpj = tempCnpj + digit;
            sum = 0;
            for (int i = 0; i < 13; i++)
                sum += int.Parse(tempCnpj[i].ToString()) * multiplier2[i];

            remainder = (sum % 11);
            if (remainder < 2)
                remainder = 0;
            else
                remainder = 11 - remainder;

            digit = digit + remainder.ToString();

            return cnpj.EndsWith(digit);
        }

        private bool BeValidLicenseNumber(string licenseNumber)
        {
            return Regex.IsMatch(licenseNumber, @"^\d{11}$");
        }

        private bool BeAValidImage(string licenseImage)
        {
            if (string.IsNullOrEmpty(licenseImage))
                return false;

            var base64Data = licenseImage.Split(',').Last();
            var imageBytes = Convert.FromBase64String(base64Data);

            try
            {
                using (var stream = new MemoryStream(imageBytes))
                {
                    IImageFormat format = Image.DetectFormat(stream);
                    return format is PngFormat || format is BmpFormat;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
```

### CreateMotorcycleDtoValidator.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Validators\CreateMotorcycleDtoValidator.cs

Descricao:

```csharp
using FluentValidation;
using MotoRent.Application.DTOs.Motorcycle;

namespace MotoRent.Application.Validators
{
    public partial class CreateMotorcycleDtoValidator : AbstractValidator<CreateMotorcycleDto>
    {
        public CreateMotorcycleDtoValidator()
        {
            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("O identificador é obrigatório.")
                .MaximumLength(50).WithMessage("O identificador não deve exceder 50 caracteres.");

            RuleFor(x => x.Year)
                .InclusiveBetween(1900, System.DateTime.Now.Year + 1)
                .WithMessage("O ano deve estar entre 1900 e o próximo ano.");

            RuleFor(x => x.Model)
                .NotEmpty().WithMessage("O modelo é obrigatório.")
                .MaximumLength(100).WithMessage("O modelo não deve exceder 100 caracteres.");

            RuleFor(x => x.LicensePlate)
                .NotEmpty().WithMessage("A placa é obrigatória.")
                .Matches(@"^[A-Z]{3}-\d{4}$").WithMessage("A placa deve estar no formato ABC-1234.");
        }
    }
}

```

### CreateRentalDtoValidator.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Validators\CreateRentalDtoValidator.cs

Descricao:

```csharp
using FluentValidation;
using MotoRent.Application.DTOs.Rental;

namespace MotoRent.Application.Validators
{
    public class CreateRentalDtoValidator : AbstractValidator<CreateRentalDto>
    {
        public CreateRentalDtoValidator()
        {
            RuleFor(x => x.DeliverymanId)
                .NotEmpty().WithMessage("O ID do entregador é obrigatório.");

            RuleFor(x => x.MotorcycleId)
                .NotEmpty().WithMessage("O ID da motocicleta é obrigatório.");

            RuleFor(x => x.Plan)
                .NotEmpty().WithMessage("O plano é obrigatório.")
                .Must(x => x == 7 || x == 15 || x == 30 || x == 45 || x == 50)
                .WithMessage("O plano deve ser de 7, 15, 30, 45 ou 50 dias.");
        }
    }
}

```

### UpdateLicenseImageDtoValidator.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\Validators\UpdateLicenseImageDtoValidator.cs

Descricao:

```csharp
using FluentValidation;
using MotoRent.Application.DTOs.Motorcycle;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;

namespace MotoRent.Application.Validators
{
    public class UpdateLicenseImageDtoValidator : AbstractValidator<UpdateLicenseImageDto>
    {
        public UpdateLicenseImageDtoValidator()
        {
            RuleFor(x => x.LicenseImage)
                .NotEmpty().WithMessage("A imagem da licença é obrigatória.")
                .Must(BeAValidImage).WithMessage("A imagem da licença deve estar em formato PNG ou BMP.");
        }

        private bool BeAValidImage(string licenseImage)
        {
            if (string.IsNullOrEmpty(licenseImage))
                return false;

            var base64Data = licenseImage.Split(',').Last();
            var imageBytes = Convert.FromBase64String(base64Data);

            try
            {
                using (var stream = new MemoryStream(imageBytes))
                {
                    IImageFormat format = Image.DetectFormat(stream);
                    return format is PngFormat || format is BmpFormat;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
```

### MongoDbContext.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\MongoDbContext.cs

Descricao:

```csharp
using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Config;
using MotoRent.Infrastructure.Data.Interfaces;

namespace MotoRent.Infrastructure.Data
{
    public class MongoDbContext : IMongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(MongoDbConfig config)
        {
            var client = new MongoClient(config.ConnectionString);
            _database = client.GetDatabase(config.DatabaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }
    }
}
```

### MongoDbServiceCollectionExtensions.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\DependencyInjection\MongoDbServiceCollectionExtensions.cs

Descricao:

```csharp
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
            services.AddScoped<INotificationRepository, NotificationRepository>();

            return services;
        }
    }
}
```

### NotFoundException.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Exceptions\NotFoundException.cs

Descricao:

```csharp
namespace MotoRent.Infrastructure.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException() : base() { }
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
        public NotFoundException(string name, object key) : base($"Entity \"{name}\" ({key}) was not found.") { }
    }
}

```

### IFileStorageService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Storage\IFileStorageService.cs

Descricao:

```csharp
namespace MotoRent.Infrastructure.Storage
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType);
        Task<Stream> DownloadFileAsync(string bucketName, string objectName);
        Task<bool> DeleteFileAsync(string bucketName, string objectName);

        Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expiryInSeconds = 3600);
        string GetPublicUrl(string bucketName, string objectName);
        Task SetBucketPublicReadPolicy(string bucketName);
        Task EnsureBucketExists(string bucketName);
    }
}

```

### MinioFileStorageService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Storage\MinioFileStorageService.cs

Descricao:

```csharp
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace MotoRent.Infrastructure.Storage
{
    public class MinioFileStorageService : IFileStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _minioEndpoint;
        public MinioFileStorageService(IConfiguration configuration)
        {
            var endpoint = $"{configuration["Minio:Host"]}:{configuration["Minio:ApiPort"]}";
            var accessKey = configuration["Minio:RootUser"];
            var secretKey = configuration["Minio:RootPassword"];
            var secure = bool.Parse(configuration["Minio:UseSsl"] ?? "false");

            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(secure)
                .Build();

            _minioEndpoint = endpoint;
        }

        public async Task<string> UploadFileAsync(string bucketName, string objectName, Stream fileStream, string contentType)
        {
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            return objectName;
        }

        public async Task<Stream> DownloadFileAsync(string bucketName, string objectName)
        {
            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClient.GetObjectAsync(getObjectArgs);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task<bool> DeleteFileAsync(string bucketName, string objectName)
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);
            return true;
        }

        public async Task<string> GetPresignedUrlAsync(string bucketName, string objectName, int expiryInSeconds = 3600)
        {
            try
            {
                var presignedGetObjectArgs = new PresignedGetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithExpiry(expiryInSeconds);

                return await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new Exception("Error generating presigned URL", ex);
            }
        }

        public async Task SetBucketPublicReadPolicy(string bucketName)
        {
            var policy = @"{
                                ""Version"": ""2012-10-17"",
                                ""Statement"": [
                                    {
                                        ""Effect"": ""Allow"",
                                        ""Principal"": { ""AWS"": [""*""] },
                                        ""Action"": [""s3:GetBucketLocation"", ""s3:ListBucket""],
                                        ""Resource"": [""arn:aws:s3:::" + bucketName + @"""]
                                    },
                                    {
                                        ""Effect"": ""Allow"",
                                        ""Principal"": { ""AWS"": [""*""] },
                                        ""Action"": [""s3:GetObject""],
                                        ""Resource"": [""arn:aws:s3:::" + bucketName + @"/*""]
                                    }
                                ]
                            }";

            var args = new SetPolicyArgs()
                .WithBucket(bucketName)
                .WithPolicy(policy);

            await _minioClient.SetPolicyAsync(args);
            Console.WriteLine($"Public read policy set for bucket '{bucketName}'.");
        }
        public string GetPublicUrl(string bucketName, string objectName)
        {
            return $"http://{_minioEndpoint}/{bucketName}/{objectName}";
        }

        public async Task EnsureBucketExists(string bucketName)
        {
            var found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!found)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            }
        }
    }
}

```

### IMotorcycleCreatedConsumer.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.MessageConsumers\Consumers\IMotorcycleCreatedConsumer.cs

Descricao:

```csharp
using MotoRent.MessageConsumers.Events;

namespace MotoRent.MessageConsumers.Consumers
{
    public interface IMotorcycleCreatedConsumer
    {
        Task ConsumeAsync(IMotorcycleCreatedEvent @event);
    }
}
```

### MotorcycleCreatedConsumer.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.MessageConsumers\Consumers\MotorcycleCreatedConsumer.cs

Descricao:

```csharp
using Microsoft.Extensions.Logging;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;
using MotoRent.MessageConsumers.Events;

namespace MotoRent.MessageConsumers.Consumers
{
    public class MotorcycleCreatedConsumer : IMotorcycleCreatedConsumer
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<MotorcycleCreatedConsumer> _logger;

        public MotorcycleCreatedConsumer(INotificationRepository notificationRepository, ILogger<MotorcycleCreatedConsumer> logger)
        {
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ConsumeAsync(IMotorcycleCreatedEvent @event)
        {
            if (@event.Year == 2024)
            {
                var notification = new NotificationModel
                {
                    Message = $"Nova moto de 2024 criada: {@event.Model} (Placa: {@event.LicensePlate})",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.CreateAsync(notification);

                _logger.LogInformation("Notificação criada para moto de 2024: {@Notification}", notification);
            }
        }
    }
}

```

### IMotorcycleCreatedEvent.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.MessageConsumers\Events\IMotorcycleCreatedEvent.cs

Descricao:

```csharp
namespace MotoRent.MessageConsumers.Events
{
    public interface IMotorcycleCreatedEvent
    {
        string Id { get; }
        string Identifier { get; }
        int Year { get; }
        string Model { get; }
        string LicensePlate { get; }
    }
}
```

### MotorcycleCreatedEvent.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.MessageConsumers\Events\MotorcycleCreatedEvent.cs

Descricao:

```csharp
namespace MotoRent.MessageConsumers.Events
{
    public class MotorcycleCreatedEvent : IMotorcycleCreatedEvent
    {
        public string Id { get; set; }
        public string Identifier { get; set; }
        public int Year { get; set; }
        public string Model { get; set; }
        public string LicensePlate { get; set; }
    }
}
```

### IMessageService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.MessageConsumers\Services\IMessageService.cs

Descricao:

```csharp
using MotoRent.MessageConsumers.Events;

namespace MotoRent.MessageConsumers.Services
{
    public interface IMessageService
    {
        Task PublishErrorLogAsync(string message);
        Task PublishAsync<T>(string topic, T message);
        Task<IMotorcycleCreatedEvent> ReceiveAsync(string topic, CancellationToken cancellationToken);
    }
}
```

### RabbitMQService.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.MessageConsumers\Services\RabbitMQService.cs

Descricao:

```csharp
using Microsoft.Extensions.Configuration;
using MotoRent.MessageConsumers.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MotoRent.MessageConsumers.Services
{
    public class RabbitMQService : IMessageService
    {
        private readonly ConnectionFactory _factory;
        private readonly string _queueName = "error_logs";

        public RabbitMQService(IConfiguration configuration)
        {
            _factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:HostName"],
                UserName = configuration["RabbitMQ:UserName"],
                Password = configuration["RabbitMQ:Password"]
            };
        }

        public async Task PublishAsync<T>(string topic, T message)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: topic,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                channel.BasicPublish(exchange: "",
                                     routingKey: topic,
                                     basicProperties: null,
                                     body: body);
            }

            await Task.CompletedTask;
        }

        public async Task PublishErrorLogAsync(string message)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: _queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: _queueName,
                                     basicProperties: null,
                                     body: body);
            }

            await Task.CompletedTask;
        }

        public async Task<IMotorcycleCreatedEvent> ReceiveAsync(string topic, CancellationToken cancellationToken)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: topic, durable: false, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                var tcs = new TaskCompletionSource<IMotorcycleCreatedEvent>();

                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var motorcycleCreatedEvent = JsonSerializer.Deserialize<MotorcycleCreatedEvent>(message);
                    tcs.SetResult(motorcycleCreatedEvent);
                };

                var consumerTag = channel.BasicConsume(queue: topic, autoAck: true, consumer: consumer);

                using (cancellationToken.Register(() => channel.BasicCancel(consumerTag)))
                {
                    return await tcs.Task;
                }
            }
        }
    }
}
```

### DeliverymanControllerTests.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.IntegrationTests\Controllers\DeliverymanControllerTests.cs

Descricao:

```csharp
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

```

### MotorcyclesControllerTests.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.IntegrationTests\Controllers\MotorcyclesControllerTests.cs

Descricao:

```csharp
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
            // Arrange
            var motorcycleCreatedEvent = new MotorcycleCreatedEvent
            {
                Id = "1",
                Identifier = "MOTO2024",
                Year = 2024,
                Model = "TestModel",
                LicensePlate = "ABC-1234"
            };

            // Act
            await _messageService.PublishAsync("motorcycle-created", motorcycleCreatedEvent);

            // Assert
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

```

### RentalsControllerTests.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.IntegrationTests\Controllers\RentalsControllerTests.cs

Descricao:

```csharp
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
```

### LoginResponse.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.IntegrationTests\Models\LoginResponse.cs

Descricao:

```csharp
namespace MotoRent.IntegrationTests.Models
{
    public class LoginResponse
    {
        public string Token { get; set; }
    }
}

```

### DeliverymanServiceTests.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.UnitTests\Services\DeliverymanServiceTests.cs

Descricao:

```csharp
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
```

### MotorcycleServiceTests.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.UnitTests\Services\MotorcycleServiceTests.cs

Descricao:

```csharp
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using MotoRent.Application.DTOs.Motorcycle;
using MotoRent.Application.Services;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;
using MotoRent.MessageConsumers.Services;

namespace MotoRent.UnitTests.Services
{
    public class MotorcycleServiceTests
    {
        private readonly Mock<IMotorcycleRepository> _mockRepository;
        private readonly Mock<IRentalRepository> _rentalRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<CreateMotorcycleDto>> _mockValidator;
        private readonly Mock<ILogger<MotorcycleService>> _mockLogger;
        private readonly MotorcycleService _service;
        private readonly Mock<IMessageService> _messageService;

        public MotorcycleServiceTests()
        {
            _mockRepository = new Mock<IMotorcycleRepository>();
            _rentalRepository = new Mock<IRentalRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockValidator = new Mock<IValidator<CreateMotorcycleDto>>();
            _mockLogger = new Mock<ILogger<MotorcycleService>>();
            _messageService = new Mock<IMessageService>();
            _service = new MotorcycleService(
                _mockRepository.Object,
                _mockMapper.Object,
                _mockValidator.Object,
                _mockLogger.Object,
                _messageService.Object, _rentalRepository.Object
            );
        }

        [Fact]
        public async Task CreateMotorcycleAsync_ValidDto_ReturnsCreatedMotorcycle()
        {
            var identifier = "MOTO123";
            var year = 2023;
            var model = "TestModel";
            var licensePlate = "ABC-1234";

            var createDto = new CreateMotorcycleDto
            {
                Identifier = identifier,
                Year = year,
                Model = model,
                LicensePlate = licensePlate
            };

            var motorcycleModel = new MotorcycleModel
            {
                Identifier = identifier,
                Year = year,
                Model = model,
                LicensePlate = licensePlate
            };

            var motorcycleDto = new MotorcycleDto
            {
                Identifier = identifier,
                Year = year,
                Model = model,
                LicensePlate = licensePlate
            };

            _mockValidator.Setup(v => v.ValidateAsync(createDto, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockMapper.Setup(m => m.Map<MotorcycleModel>(createDto)).Returns(motorcycleModel);
            _mockMapper.Setup(m => m.Map<MotorcycleDto>(motorcycleModel)).Returns(motorcycleDto);

            _mockRepository.Setup(r => r.CreateAsync(motorcycleModel)).ReturnsAsync(motorcycleModel);

            var result = await _service.CreateMotorcycleAsync(createDto);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(motorcycleDto, options => options.ExcludingMissingMembers());

            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<MotorcycleModel>()), Times.Once);
            _mockValidator.Verify(v => v.ValidateAsync(createDto, default), Times.Once);
        }


        [Fact]
        public async Task GetMotorcycleByIdAsync_ExistingId_ReturnsMotorcycle()
        {
            var motorcycleId = "1";
            var identifier = "MOTO123";
            var year = 2023;
            var model = "TestModel";
            var licensePlate = "ABC-1234";

            var motorcycleModel = new MotorcycleModel
            {
                Id = motorcycleId,
                Identifier = identifier,
                Year = year,
                Model = model,
                LicensePlate = licensePlate
            };

            var motorcycleDto = new MotorcycleDto
            {
                Identifier = identifier,
                Year = year,
                Model = model,
                LicensePlate = licensePlate
            };

            _mockRepository.Setup(r => r.GetByIdAsync(motorcycleId))
                .ReturnsAsync(motorcycleModel);

            _mockMapper.Setup(m => m.Map<MotorcycleDto>(motorcycleModel))
                .Returns(motorcycleDto);

            var result = await _service.GetMotorcycleByIdAsync(motorcycleId);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(motorcycleDto, options => options.ExcludingMissingMembers());

            _mockRepository.Verify(r => r.GetByIdAsync(motorcycleId), Times.Once);
        }


        [Fact]
        public async Task GetMotorcycleByIdAsync_NonExistingId_ThrowsArgumentException()
        {
            var nonExistingId = "999";

            _mockRepository.Setup(r => r.GetByIdAsync(nonExistingId))
                .ReturnsAsync((MotorcycleModel)null);

            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.GetMotorcycleByIdAsync(nonExistingId));

            exception.Message.Should().Contain("Motorcycle not found");

            _mockRepository.Verify(r => r.GetByIdAsync(nonExistingId), Times.Once);
        }


        [Fact]
        public async Task GetAllMotorcyclesAsync_ReturnsAllMotorcycles()
        {
            var motorcycles = new List<MotorcycleModel>
    {
        new MotorcycleModel { Id = "1", Identifier = "MOTO1", Year = 2023, Model = "Model1", LicensePlate = "ABC-1234" },
        new MotorcycleModel { Id = "2", Identifier = "MOTO2", Year = 2022, Model = "Model2", LicensePlate = "DEF-5678" }
    };

            var motorcycleDtos = new List<MotorcycleDto>
    {
        new MotorcycleDto { Identifier = "MOTO1", Year = 2023, Model = "Model1", LicensePlate = "ABC-1234" },
        new MotorcycleDto { Identifier = "MOTO2", Year = 2022, Model = "Model2", LicensePlate = "DEF-5678" }
    };

            _mockRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(motorcycles);

            _mockMapper.Setup(m => m.Map<IEnumerable<MotorcycleDto>>(motorcycles))
                .Returns(motorcycleDtos);

            var result = await _service.GetAllMotorcyclesAsync();

            result.Should().NotBeNull();
            result.Should().HaveCount(motorcycleDtos.Count);
            result.Should().BeEquivalentTo(motorcycleDtos);

            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }


        [Fact]
        public async Task UpdateMotorcycleLicensePlateAsync_ValidId_UpdatesLicensePlate()
        {
            var motorcycleId = "1";
            var oldLicensePlate = "ABC-1234";
            var newLicensePlate = "XYZ-9876";

            var updateLicensePlateDto = new UpdateLicensePlateDto { LicensePlate = newLicensePlate };

            var existingMotorcycle = new MotorcycleModel
            {
                Id = motorcycleId,
                Identifier = "MOTO1",
                Year = 2023,
                Model = "TestModel",
                LicensePlate = oldLicensePlate
            };

            _mockRepository.Setup(r => r.GetByIdAsync(motorcycleId))
                .ReturnsAsync(existingMotorcycle);

            _mockRepository.Setup(r => r.UpdateAsync(motorcycleId, It.IsAny<MotorcycleModel>()))
                .Returns(Task.CompletedTask);

            await _service.UpdateMotorcycleLicensePlateAsync(motorcycleId, updateLicensePlateDto);

            _mockRepository.Verify(r => r.GetByIdAsync(motorcycleId), Times.Once);

            _mockRepository.Verify(r => r.UpdateAsync(motorcycleId,
                It.Is<MotorcycleModel>(m =>
                    m.LicensePlate == newLicensePlate)), Times.Once);
        }


        [Fact]
        public async Task UpdateMotorcycleLicensePlateAsync_InvalidId_ThrowsArgumentException()
        {
            var invalidId = "999";
            var newLicensePlate = "XYZ-9876";
            var updateLicensePlateDto = new UpdateLicensePlateDto { LicensePlate = newLicensePlate };

            _mockRepository.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync((MotorcycleModel)null);

            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.UpdateMotorcycleLicensePlateAsync(invalidId, updateLicensePlateDto));

            exception.Message.Should().Contain("Motorcycle not found");

            _mockRepository.Verify(r => r.GetByIdAsync(invalidId), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<MotorcycleModel>()), Times.Never);
        }


        [Fact]
        public async Task DeleteMotorcycleAsync_ExistingId_DeletesMotorcycle()
        {
            var motorcycleId = "1";

            _mockRepository.Setup(r => r.DeleteAsync(motorcycleId))
                .Returns(Task.CompletedTask);

            await _service.DeleteMotorcycleAsync(motorcycleId);

            _mockRepository.Verify(r => r.DeleteAsync(motorcycleId), Times.Once);
        }


        [Fact]
        public async Task GetMotorcyclesByLicensePlateAsync_ExistingLicensePlate_ReturnsMotorcycles()
        {
            var licensePlate = "ABC-1234";
            var motorcycles = new List<MotorcycleModel>
    {
        new MotorcycleModel { Id = "1", Identifier = "MOTO1", Year = 2023, Model = "Model1", LicensePlate = licensePlate }
    };

            var motorcycleDtos = new List<MotorcycleDto>
    {
        new MotorcycleDto { Identifier = "MOTO1", Year = 2023, Model = "Model1", LicensePlate = licensePlate }
    };

            _mockRepository.Setup(r => r.GetByLicensePlateAsync(licensePlate))
                .ReturnsAsync(motorcycles);

            _mockMapper.Setup(m => m.Map<IEnumerable<MotorcycleDto>>(motorcycles))
                .Returns(motorcycleDtos);

            var result = await _service.GetMotorcyclesByLicensePlateAsync(licensePlate);

            result.Should().NotBeNull();
            result.Should().HaveCount(motorcycleDtos.Count);
            result.Should().BeEquivalentTo(motorcycleDtos);

            _mockRepository.Verify(r => r.GetByLicensePlateAsync(licensePlate), Times.Once);
        }

    }
}
```

### RentalServiceTests.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.UnitTests\Services\RentalServiceTests.cs

Descricao:

```csharp
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using MotoRent.Application.DTOs.Rental;
using MotoRent.Application.Services;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.UnitTests.Services
{
    public class RentalServiceTests
    {
        private readonly Mock<IRentalRepository> _mockRentalRepository;
        private readonly Mock<IDeliverymanRepository> _mockDeliverymanRepository;
        private readonly Mock<IMotorcycleRepository> _mockMotorcycleRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<CreateRentalDto>> _mockCreateRentalValidator;
        private readonly Mock<ILogger<RentalService>> _mockLogger;
        private readonly RentalService _service;

        public RentalServiceTests()
        {
            _mockRentalRepository = new Mock<IRentalRepository>();
            _mockDeliverymanRepository = new Mock<IDeliverymanRepository>();
            _mockMotorcycleRepository = new Mock<IMotorcycleRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockCreateRentalValidator = new Mock<IValidator<CreateRentalDto>>();
            _mockLogger = new Mock<ILogger<RentalService>>();

            _service = new RentalService(
                _mockRentalRepository.Object,
                _mockDeliverymanRepository.Object,
                _mockMotorcycleRepository.Object,
                _mockMapper.Object,
                _mockCreateRentalValidator.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task CreateRentalAsync_ValidDto_ReturnsCreatedRental()
        {
            var createDto = new CreateRentalDto
            {
                DeliverymanId = "1",
                MotorcycleId = "1",
                Plan = 7
            };

            var deliverymanModel = new DeliverymanModel
            {
                Id = "1",
                LicenseType = "A"
            };

            var motorcycleModel = new MotorcycleModel
            {
                Id = "1"
            };

            var rentalModel = new RentalModel
            {
                Id = "1",
                DeliverymanId = "1",
                MotorcycleId = "1",
                DailyRate = 30.0m,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(8),
                ExpectedEndDate = DateTime.UtcNow.Date.AddDays(8),
                Plan = 7
            };

            var rentalDto = new RentalDto
            {
                DeliverymanId = "1",
                MotorcycleId = "1",
                DailyRate = 30.0m,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(8),
                ExpectedEndDate = DateTime.UtcNow.Date.AddDays(8),
                Plan = 7
            };

            _mockCreateRentalValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateRentalDto>(), default))
                .ReturnsAsync(new ValidationResult());

            _mockDeliverymanRepository.Setup(r => r.GetByIdAsync(createDto.DeliverymanId))
                .ReturnsAsync(deliverymanModel);

            _mockMotorcycleRepository.Setup(r => r.GetByIdAsync(createDto.MotorcycleId))
                .ReturnsAsync(motorcycleModel);

            _mockMapper.Setup(m => m.Map<RentalModel>(createDto)).Returns(rentalModel);
            _mockMapper.Setup(m => m.Map<RentalDto>(rentalModel)).Returns(rentalDto);

            _mockRentalRepository.Setup(r => r.CreateAsync(It.IsAny<RentalModel>()))
                .ReturnsAsync(rentalModel);

            var result = await _service.CreateRentalAsync(createDto);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(rentalDto);
            _mockRentalRepository.Verify(r => r.CreateAsync(It.IsAny<RentalModel>()), Times.Once);
        }

        [Fact]
        public async Task CreateRentalAsync_InvalidDeliverymanLicense_ThrowsArgumentException()
        {
            var createDto = new CreateRentalDto
            {
                DeliverymanId = "1",
                MotorcycleId = "1",
                Plan = 7
            };

            var deliverymanModel = new DeliverymanModel
            {
                Id = "1",
                LicenseType = "B"
            };

            _mockCreateRentalValidator.Setup(v => v.ValidateAsync(It.IsAny<CreateRentalDto>(), default))
                .ReturnsAsync(new ValidationResult());

            _mockDeliverymanRepository.Setup(r => r.GetByIdAsync(createDto.DeliverymanId))
                .ReturnsAsync(deliverymanModel);

            await _service.Invoking(s => s.CreateRentalAsync(createDto))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("Deliveryman must have A or AB license type");
        }

        [Fact]
        public async Task GetRentalByIdAsync_ExistingId_ReturnsRental()
        {
            var rentalId = "1";
            var rentalModel = new RentalModel
            {
                Id = rentalId,
                DeliverymanId = "1",
                MotorcycleId = "1",
                DailyRate = 30.0m,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(7),
                ExpectedEndDate = DateTime.UtcNow.Date.AddDays(7),
                Plan = 7
            };

            var rentalDto = new RentalDto
            {

                DeliverymanId = "1",
                MotorcycleId = "1",
                DailyRate = 30.0m,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(7),
                ExpectedEndDate = DateTime.UtcNow.Date.AddDays(7),
                Plan = 7
            };

            _mockRentalRepository.Setup(r => r.GetByIdAsync(rentalId))
                .ReturnsAsync(rentalModel);

            _mockMapper.Setup(m => m.Map<RentalDto>(rentalModel))
                .Returns(rentalDto);

            var result = await _service.GetRentalByIdAsync(rentalId);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(rentalDto);
            _mockRentalRepository.Verify(r => r.GetByIdAsync(rentalId), Times.Once);
        }

        [Fact]
        public async Task CalculateRentalCostAsync_EarlyReturn_AppliesPenalty()
        {
            var rentalId = "1";
            var updateReturnDateDto = new UpdateReturnDateDto { ReturnDate = DateTime.UtcNow.Date.AddDays(5) };
            var rentalModel = new RentalModel
            {
                Id = rentalId,
                DailyRate = 30.0m,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(7),
                ExpectedEndDate = DateTime.UtcNow.Date.AddDays(7),
                Plan = 7
            };

            _mockRentalRepository.Setup(r => r.GetByIdAsync(rentalId))
                .ReturnsAsync(rentalModel);

            _mockRentalRepository.Setup(r => r.UpdateReturnDateAsync(rentalId, updateReturnDateDto.ReturnDate, null))
                .Returns(Task.CompletedTask);

            var result = await _service.CalculateRentalCostAsync(rentalId, updateReturnDateDto);

            result.Should().NotBeNull();
            result.TotalCost.Should().Be(162.0m);
            result.Message.Should().Contain("Early return");
            _mockRentalRepository.Verify(r => r.UpdateReturnDateAsync(rentalId, updateReturnDateDto.ReturnDate, null), Times.Once);
        }

        [Fact]
        public async Task CalculateRentalCostAsync_LateReturn_AppliesExtraCharge()
        {
            var rentalId = "1";
            var updateReturnDateDto = new UpdateReturnDateDto { ReturnDate = DateTime.UtcNow.Date.AddDays(9) };
            var rentalModel = new RentalModel
            {
                Id = rentalId,
                DailyRate = 30.0m,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(7),
                ExpectedEndDate = DateTime.UtcNow.Date.AddDays(7),
                Plan = 7
            };

            _mockRentalRepository.Setup(r => r.GetByIdAsync(rentalId))
                .ReturnsAsync(rentalModel);

            _mockRentalRepository.Setup(r => r.UpdateReturnDateAsync(rentalId, updateReturnDateDto.ReturnDate, null))
                .Returns(Task.CompletedTask);

            var result = await _service.CalculateRentalCostAsync(rentalId, updateReturnDateDto);

            result.Should().NotBeNull();
            result.TotalCost.Should().Be(310.0m);
            result.Message.Should().Contain("Late return");
            _mockRentalRepository.Verify(r => r.UpdateReturnDateAsync(rentalId, updateReturnDateDto.ReturnDate, null), Times.Once);
        }
    }
}
```

### ErrorResponseDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Default\ErrorResponseDto.cs

Descricao:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Default
{
    public class ErrorResponseDto
    {
        [JsonPropertyName("mensagem")]
        public string Message { get; set; }
    }
}

```

### CreateDeliverymanDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Deliveryman\CreateDeliverymanDto.cs

Descricao:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Deliveryman
{
    public class CreateDeliverymanDto
    {
        [JsonPropertyName("identificador")]
        public string Identifier { get; set; }

        [JsonPropertyName("nome")]
        public string Name { get; set; }

        [JsonPropertyName("cnpj")]
        public string CNPJ { get; set; }

        [JsonPropertyName("data_nascimento")]
        public DateTime BirthDate { get; set; }

        [JsonPropertyName("numero_cnh")]
        public string LicenseNumber { get; set; }

        [JsonPropertyName("tipo_cnh")]
        public string LicenseType { get; set; }

        [JsonPropertyName("imagem_cnh")]
        public string LicenseImage { get; set; }
    }
}
```

### DeliverymanDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Deliveryman\DeliverymanDto.cs

Descricao:

```csharp
namespace MotoRent.Application.DTOs.Deliveryman
{
    public class DeliverymanDto
    {
        public string Id { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
        public string CNPJ { get; set; }
        public DateTime BirthDate { get; set; }
        public string LicenseNumber { get; set; }
        public string LicenseType { get; set; }
        public string LicenseImage { get; set; }
    }
}
```

### CreateMotorcycleDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Motorcycle\CreateMotorcycleDto.cs

Descricao:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Motorcycle
{
    public class CreateMotorcycleDto
    {
        [JsonPropertyName("identificador")]
        public string Identifier { get; set; }

        [JsonPropertyName("ano")]
        public int Year { get; set; }

        [JsonPropertyName("modelo")]
        public string Model { get; set; }

        [JsonPropertyName("placa")]
        public string LicensePlate { get; set; }
    }
}
```

### MotorcycleDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Motorcycle\MotorcycleDto.cs

Descricao:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Motorcycle
{
    public class MotorcycleDto
    {
        [JsonPropertyName("identificador")]
        public string Identifier { get; set; }

        [JsonPropertyName("ano")]
        public int Year { get; set; }

        [JsonPropertyName("modelo")]
        public string Model { get; set; }

        [JsonPropertyName("placa")]
        public string LicensePlate { get; set; }
    }
}
```

### SuccessMotorcycleResponseDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Motorcycle\SuccessMotorcycleResponseDto.cs

Descricao:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Motorcycle
{
    public class SuccessMotorcycleResponseDto
    {
        [JsonPropertyName("mensagem")]
        public string Message { get; set; }
    }
}
```

### UpdateLicenseImageDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Motorcycle\UpdateLicenseImageDto.cs

Descricao:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Motorcycle
{
    public class UpdateLicenseImageDto
    {
        [JsonPropertyName("imagem_cnh")]
        public string LicenseImage { get; set; }
    }
}
```

### UpdateLicensePlateDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Motorcycle\UpdateLicensePlateDto.cs

Descricao:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Motorcycle
{
    public class UpdateLicensePlateDto
    {
        [JsonPropertyName("placa")]
        public string LicensePlate { get; set; }
    }
}
```

### CreateRentalDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Rental\CreateRentalDto.cs

Descricao:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Rental
{
    public class CreateRentalDto
    {
        [JsonPropertyName("identificador")]
        public string Identifier { get; set; }

        [JsonPropertyName("entregador_id")]
        public string DeliverymanId { get; set; }

        [JsonPropertyName("moto_id")]
        public string MotorcycleId { get; set; }

        [JsonPropertyName("data_inicio")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("data_termino")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("data_previsao_termino")]
        public DateTime ExpectedEndDate { get; set; }

        [JsonPropertyName("plano")]
        public int Plan { get; set; }
    }
}
```

### RentalCalculationResultDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Rental\RentalCalculationResultDto.cs

Descricao:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Rental
{
    public class RentalCalculationResultDto
    {
        [JsonIgnore]
        public decimal TotalCost { get; set; }
        [JsonPropertyName("mensagem")]
        public string Message { get; set; }
    }
}
```

### RentalDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Rental\RentalDto.cs

Descricao:

```csharp
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Rental
{
    public class RentalDto
    {
        [JsonPropertyName("identificador")]
        public string Identifier { get; set; }

        [JsonPropertyName("valor_diaria")]
        public decimal DailyRate { get; set; }

        [JsonPropertyName("entregador_id")]
        public string DeliverymanId { get; set; }

        [JsonPropertyName("moto_id")]
        public string MotorcycleId { get; set; }

        [JsonPropertyName("data_inicio")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("data_termino")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("data_previsao_termino")]
        public DateTime ExpectedEndDate { get; set; }

        [JsonPropertyName("data_devolucao")]
        public DateTime? ReturnDate { get; set; }

        [BsonElement("plan")]
        public int Plan { get; set; }
    }
}
```

### UpdateReturnDateDto.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\DTOs\Rental\UpdateReturnDateDto.cs

Descricao:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Rental
{
    public class UpdateReturnDateDto
    {
        [JsonPropertyName("data_devolucao")]
        public DateTime ReturnDate { get; set; }
    }
}
```

### MongoDbConfig.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Config\MongoDbConfig.cs

Descricao:

```csharp
namespace MotoRent.Infrastructure.Data.Config
{
    public class MongoDbConfig
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }


    }
}
```

### IDeliverymanRepository.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Interfaces\IDeliverymanRepository.cs

Descricao:

```csharp
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IDeliverymanRepository : IRepository<DeliverymanModel>
    {
        Task<DeliverymanModel> GetByCNPJAsync(string cnpj);
        Task<DeliverymanModel> GetByIdentifierAsync(string id);
        Task<DeliverymanModel> GetByLicenseNumberAsync(string licenseNumber);
        Task UpdateLicenseImageAsync(string id, string licenseImage);
    }
}

```

### IMessagePublisher.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Interfaces\IMessagePublisher.cs

Descricao:

```csharp
namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(string topic, T message);
    }
}
```

### IMongoDbContext.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Interfaces\IMongoDbContext.cs

Descricao:

```csharp
using MongoDB.Driver;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IMongoDbContext
    {
        IMongoCollection<T> GetCollection<T>(string name);
    }
}
```

### IMotorcycleRepository.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Interfaces\IMotorcycleRepository.cs

Descricao:

```csharp
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IMotorcycleRepository : IRepository<MotorcycleModel>
    {
        Task<MotorcycleModel> GetByIdentifierAsync(string id);
        Task<IEnumerable<MotorcycleModel>> GetByLicensePlateAsync(string licensePlate);
    }
}

```

### INotificationRepository.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Interfaces\INotificationRepository.cs

Descricao:

```csharp
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface INotificationRepository
    {
        Task<NotificationModel> CreateAsync(NotificationModel notification);
        Task<IEnumerable<NotificationModel>> GetAllAsync();
        Task<NotificationModel> GetByIdAsync(string id);
        Task<IEnumerable<NotificationModel>> GetRecentNotificationsAsync(int count);
        Task UpdateAsync(string id, NotificationModel notification);
        Task DeleteAsync(string id);
        Task<long> GetNotificationCountAsync();
    }
}
```

### IRentalRepository.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Interfaces\IRentalRepository.cs

Descricao:

```csharp
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IRentalRepository : IRepository<RentalModel>
    {
        Task<bool> ExistsForMotorcycleAsync(string motorcycleId);
        Task<RentalModel> GetByIdentifierAsync(string identifier);
        Task UpdateReturnDateAsync(string id, DateTime returnDate, decimal? totalCost);
    }
}

```

### IRepository.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Interfaces\IRepository.cs

Descricao:

```csharp
namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(string id);
        Task<T> GetByFieldStringAsync(string fieldName, string value);
        Task<T> CreateAsync(T entity);
        Task UpdateAsync(string id, T entity);
        Task UpdateByFieldAsync(string fieldName, string id, T entity);
        Task DeleteAsync(string id);
    }
}

```

### DeliverymanModel.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Models\DeliverymanModel.cs

Descricao:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MotoRent.Infrastructure.Data.Models
{
    public class DeliverymanModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("identifier")]
        public string Identifier { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("cnpj")]
        public string CNPJ { get; set; }

        [BsonElement("birth_date")]
        public DateTime BirthDate { get; set; }

        [BsonElement("license_number")]
        public string LicenseNumber { get; set; }

        [BsonElement("license_type")]
        public string LicenseType { get; set; }

        [BsonElement("license_image")]
        public string LicenseImage { get; set; }
    }
}
```

### MotorcycleModel.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Models\MotorcycleModel.cs

Descricao:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MotoRent.Infrastructure.Data.Models
{
    public class MotorcycleModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("identifier")]
        public string Identifier { get; set; }

        [BsonElement("year")]
        public int Year { get; set; }

        [BsonElement("model")]
        public string Model { get; set; }

        [BsonElement("license_plate")]
        public string LicensePlate { get; set; }
    }
}
```

### NotificationModel.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Models\NotificationModel.cs

Descricao:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MotoRent.Infrastructure.Data.Models
{
    public class NotificationModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("message")]
        public string Message { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
```

### RentalModel.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Models\RentalModel.cs

Descricao:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MotoRent.Infrastructure.Data.Models
{
    public class RentalModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("identifier")]
        public string Identifier { get; set; }

        [BsonElement("daily_rate")]
        public decimal DailyRate { get; set; }

        [BsonElement("deliveryman_id")]
        public string DeliverymanId { get; set; }

        [BsonElement("motorcycle_id")]
        public string MotorcycleId { get; set; }

        [BsonElement("start_date")]
        public DateTime StartDate { get; set; }

        [BsonElement("end_date")]
        public DateTime EndDate { get; set; }

        [BsonElement("expected_end_date")]
        public DateTime ExpectedEndDate { get; set; }

        [BsonElement("return_date")]
        public DateTime? ReturnDate { get; set; }

        [BsonElement("plan")]
        public int Plan { get; set; }

        [BsonElement("total_cost")]
        public decimal? TotalCost { get; set; }
    }
}
```

### BaseRepository.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Repositories\BaseRepository.cs

Descricao:

```csharp
using MongoDB.Bson;
using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Interfaces;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly IMongoCollection<T> _collection;

        protected BaseRepository(IMongoDbContext context, string collectionName)
        {
            _collection = context.GetCollection<T>(collectionName);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public virtual async Task<T> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
            {
                return null;
            }
            var filter = Builders<T>.Filter.Eq("_id", objectId);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public virtual async Task<T> GetByFieldStringAsync(string fieldName, string value)
        {
            var filter = Builders<T>.Filter.Eq(fieldName, value);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public virtual async Task<T> CreateAsync(T entity)
        {
            await _collection.InsertOneAsync(entity);
            return entity;
        }

        public virtual async Task UpdateAsync(string id, T entity)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
                return;

            var filter = Builders<T>.Filter.Eq("_id", objectId);
            await _collection.ReplaceOneAsync(filter, entity);
        }

        public virtual async Task UpdateByFieldAsync(string fieldName, string id, T entity)
        {


            var filter = Builders<T>.Filter.Eq(fieldName, id);
            await _collection.ReplaceOneAsync(filter, entity);
        }

        public virtual async Task DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
                return;
            var filter = Builders<T>.Filter.Eq("_id", objectId);
            await _collection.DeleteOneAsync(filter);
        }

        public virtual async Task DeleteAsync(string fieldName, string id)
        {
            var filter = Builders<T>.Filter.Eq(fieldName, id);
            await _collection.DeleteOneAsync(filter);
        }
    }
}
```

### DeliverymanRepository.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Repositories\DeliverymanRepository.cs

Descricao:

```csharp
using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public class DeliverymanRepository : BaseRepository<DeliverymanModel>, IDeliverymanRepository
    {
        public DeliverymanRepository(IMongoDbContext context) : base(context, "deliverymen")
        {
        }

        public async Task<DeliverymanModel> GetByIdentifierAsync(string id)
        {
            var filter = Builders<DeliverymanModel>.Filter.Eq(d => d.Identifier, id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<DeliverymanModel> GetByCNPJAsync(string cnpj)
        {
            var filter = Builders<DeliverymanModel>.Filter.Eq(d => d.CNPJ, cnpj);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<DeliverymanModel> GetByLicenseNumberAsync(string licenseNumber)
        {
            var filter = Builders<DeliverymanModel>.Filter.Eq(d => d.LicenseNumber, licenseNumber);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task UpdateLicenseImageAsync(string id, string licenseImage)
        {
            var filter = Builders<DeliverymanModel>.Filter.Eq("identifier", id);
            var update = Builders<DeliverymanModel>.Update.Set(d => d.LicenseImage, licenseImage);
            await _collection.UpdateOneAsync(filter, update);
        }
    }
}
```

### MotorcycleRepository.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Repositories\MotorcycleRepository.cs

Descricao:

```csharp
using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public class MotorcycleRepository : BaseRepository<MotorcycleModel>, IMotorcycleRepository
    {
        public MotorcycleRepository(IMongoDbContext context) : base(context, "motorcycles")
        {
        }
        public async Task<MotorcycleModel> GetByIdentifierAsync(string id)
        {
            var filter = Builders<MotorcycleModel>.Filter.Eq(d => d.Identifier, id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<MotorcycleModel>> GetByLicensePlateAsync(string licensePlate)
        {
            var filter = Builders<MotorcycleModel>.Filter.Eq(m => m.LicensePlate, licensePlate);
            return await _collection.Find(filter).ToListAsync();
        }
    }
}
```

### NotificationRepository.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Repositories\NotificationRepository.cs

Descricao:

```csharp
using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IMongoCollection<NotificationModel> _collection;

        public NotificationRepository(IMongoDbContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _collection = context.GetCollection<NotificationModel>("notifications");
        }

        public async Task<NotificationModel> CreateAsync(NotificationModel notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            await _collection.InsertOneAsync(notification);
            return notification;
        }

        public async Task<IEnumerable<NotificationModel>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<NotificationModel> GetByIdAsync(string id)
        {
            return await _collection.Find(n => n.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<NotificationModel>> GetRecentNotificationsAsync(int count)
        {
            return await _collection.Find(_ => true)
                                    .SortByDescending(n => n.CreatedAt)
                                    .Limit(count)
                                    .ToListAsync();
        }

        public async Task UpdateAsync(string id, NotificationModel notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            await _collection.ReplaceOneAsync(n => n.Id == id, notification);
        }

        public async Task DeleteAsync(string id)
        {
            await _collection.DeleteOneAsync(n => n.Id == id);
        }

        public async Task<long> GetNotificationCountAsync()
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }
    }
}
```

### RentalRepository.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\Data\Repositories\RentalRepository.cs

Descricao:

```csharp
using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public class RentalRepository : BaseRepository<RentalModel>, IRentalRepository
    {
        public RentalRepository(IMongoDbContext context) : base(context, "rentals")
        {
        }

        public async Task<RentalModel> GetByIdentifierAsync(string identifier)
        {
            var filter = Builders<RentalModel>.Filter.Eq(r => r.Identifier, identifier);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        // Método existente
        public async Task UpdateReturnDateAsync(string id, DateTime returnDate, decimal? totalCost)
        {
            var filter = Builders<RentalModel>.Filter.Eq(r => r.Identifier, id);
            var update = Builders<RentalModel>.Update
                .Set(r => r.ReturnDate, returnDate)
                .Set(r => r.TotalCost, totalCost);
            await _collection.UpdateOneAsync(filter, update);
        }

        public async Task<bool> ExistsForMotorcycleAsync(string motorcycleId)
        {
            var filter = Builders<RentalModel>.Filter.Eq(r => r.MotorcycleId, motorcycleId);
            return await _collection.CountDocumentsAsync(filter) > 0;
        }
    }
}

```

### .NETCoreApp,Version=v8.0.AssemblyAttributes.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\obj\Debug\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs

Descricao:

```csharp
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]

```

### MotoRent.API.AssemblyInfo.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\obj\Debug\net8.0\MotoRent.API.AssemblyInfo.cs

Descricao:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute("8339d64c-c876-421b-a5bd-cae47255d8ee")]
[assembly: System.Reflection.AssemblyCompanyAttribute("MotoRent.API")]
[assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")]
[assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+64cd47327392c08c41903fa05e6d63212e80a7b1")]
[assembly: System.Reflection.AssemblyProductAttribute("MotoRent.API")]
[assembly: System.Reflection.AssemblyTitleAttribute("MotoRent.API")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]

// Generated by the MSBuild WriteCodeFragment class.


```

### MotoRent.API.GlobalUsings.g.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\obj\Debug\net8.0\MotoRent.API.GlobalUsings.g.cs

Descricao:

```csharp
// <auto-generated/>
global using global::Microsoft.AspNetCore.Builder;
global using global::Microsoft.AspNetCore.Hosting;
global using global::Microsoft.AspNetCore.Http;
global using global::Microsoft.AspNetCore.Routing;
global using global::Microsoft.Extensions.Configuration;
global using global::Microsoft.Extensions.DependencyInjection;
global using global::Microsoft.Extensions.Hosting;
global using global::Microsoft.Extensions.Logging;
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Net.Http.Json;
global using global::System.Threading;
global using global::System.Threading.Tasks;

```

### MotoRent.API.MvcApplicationPartsAssemblyInfo.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.API\obj\Debug\net8.0\MotoRent.API.MvcApplicationPartsAssemblyInfo.cs

Descricao:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute("FluentValidation.AspNetCore")]
[assembly: Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute("Microsoft.AspNetCore.OpenApi")]
[assembly: Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute("MotoRent.Application")]
[assembly: Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartAttribute("Swashbuckle.AspNetCore.SwaggerGen")]

// Generated by the MSBuild WriteCodeFragment class.


```

### .NETCoreApp,Version=v8.0.AssemblyAttributes.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\obj\Debug\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs

Descricao:

```csharp
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]

```

### MotoRent.Application.AssemblyInfo.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\obj\Debug\net8.0\MotoRent.Application.AssemblyInfo.cs

Descricao:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyCompanyAttribute("MotoRent.Application")]
[assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")]
[assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+64cd47327392c08c41903fa05e6d63212e80a7b1")]
[assembly: System.Reflection.AssemblyProductAttribute("MotoRent.Application")]
[assembly: System.Reflection.AssemblyTitleAttribute("MotoRent.Application")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]

// Generated by the MSBuild WriteCodeFragment class.


```

### MotoRent.Application.GlobalUsings.g.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Application\obj\Debug\net8.0\MotoRent.Application.GlobalUsings.g.cs

Descricao:

```csharp
// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;

```

### .NETCoreApp,Version=v8.0.AssemblyAttributes.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Domain\obj\Debug\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs

Descricao:

```csharp
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]

```

### MotoRent.Domain.AssemblyInfo.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Domain\obj\Debug\net8.0\MotoRent.Domain.AssemblyInfo.cs

Descricao:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyCompanyAttribute("MotoRent.Domain")]
[assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")]
[assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+bbf75f8769233fcd0b608842007c5acc261e7f75")]
[assembly: System.Reflection.AssemblyProductAttribute("MotoRent.Domain")]
[assembly: System.Reflection.AssemblyTitleAttribute("MotoRent.Domain")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]

// Generated by the MSBuild WriteCodeFragment class.


```

### MotoRent.Domain.GlobalUsings.g.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Domain\obj\Debug\net8.0\MotoRent.Domain.GlobalUsings.g.cs

Descricao:

```csharp
// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;

```

### .NETCoreApp,Version=v8.0.AssemblyAttributes.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\obj\Debug\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs

Descricao:

```csharp
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]

```

### MotoRent.Infrastructure.AssemblyInfo.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\obj\Debug\net8.0\MotoRent.Infrastructure.AssemblyInfo.cs

Descricao:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyCompanyAttribute("MotoRent.Infrastructure")]
[assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")]
[assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+64cd47327392c08c41903fa05e6d63212e80a7b1")]
[assembly: System.Reflection.AssemblyProductAttribute("MotoRent.Infrastructure")]
[assembly: System.Reflection.AssemblyTitleAttribute("MotoRent.Infrastructure")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]

// Generated by the MSBuild WriteCodeFragment class.


```

### MotoRent.Infrastructure.GlobalUsings.g.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Infrastructure\obj\Debug\net8.0\MotoRent.Infrastructure.GlobalUsings.g.cs

Descricao:

```csharp
// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;

```

### .NETCoreApp,Version=v8.0.AssemblyAttributes.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.MessageConsumers\obj\Debug\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs

Descricao:

```csharp
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]

```

### MotoRent.MessageConsumers.AssemblyInfo.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.MessageConsumers\obj\Debug\net8.0\MotoRent.MessageConsumers.AssemblyInfo.cs

Descricao:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyCompanyAttribute("MotoRent.MessageConsumers")]
[assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")]
[assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+64cd47327392c08c41903fa05e6d63212e80a7b1")]
[assembly: System.Reflection.AssemblyProductAttribute("MotoRent.MessageConsumers")]
[assembly: System.Reflection.AssemblyTitleAttribute("MotoRent.MessageConsumers")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]

// Generated by the MSBuild WriteCodeFragment class.


```

### MotoRent.MessageConsumers.GlobalUsings.g.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.MessageConsumers\obj\Debug\net8.0\MotoRent.MessageConsumers.GlobalUsings.g.cs

Descricao:

```csharp
// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;

```

### .NETCoreApp,Version=v8.0.AssemblyAttributes.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Shared\obj\Debug\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs

Descricao:

```csharp
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]

```

### MotoRent.Shared.AssemblyInfo.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Shared\obj\Debug\net8.0\MotoRent.Shared.AssemblyInfo.cs

Descricao:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyCompanyAttribute("MotoRent.Shared")]
[assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")]
[assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+bbf75f8769233fcd0b608842007c5acc261e7f75")]
[assembly: System.Reflection.AssemblyProductAttribute("MotoRent.Shared")]
[assembly: System.Reflection.AssemblyTitleAttribute("MotoRent.Shared")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]

// Generated by the MSBuild WriteCodeFragment class.


```

### MotoRent.Shared.GlobalUsings.g.cs
Path: D:\TestesTrabalho\MotoRentAPI\src\MotoRent.Shared\obj\Debug\net8.0\MotoRent.Shared.GlobalUsings.g.cs

Descricao:

```csharp
// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;

```

### .NETCoreApp,Version=v8.0.AssemblyAttributes.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.IntegrationTests\obj\Debug\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs

Descricao:

```csharp
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]

```

### MotoRent.IntegrationTests.AssemblyInfo.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.IntegrationTests\obj\Debug\net8.0\MotoRent.IntegrationTests.AssemblyInfo.cs

Descricao:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyCompanyAttribute("MotoRent.IntegrationTests")]
[assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")]
[assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+64cd47327392c08c41903fa05e6d63212e80a7b1")]
[assembly: System.Reflection.AssemblyProductAttribute("MotoRent.IntegrationTests")]
[assembly: System.Reflection.AssemblyTitleAttribute("MotoRent.IntegrationTests")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]

// Generated by the MSBuild WriteCodeFragment class.


```

### MotoRent.IntegrationTests.GlobalUsings.g.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.IntegrationTests\obj\Debug\net8.0\MotoRent.IntegrationTests.GlobalUsings.g.cs

Descricao:

```csharp
// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;
global using global::Xunit;

```

### .NETCoreApp,Version=v8.0.AssemblyAttributes.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.UnitTests\obj\Debug\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs

Descricao:

```csharp
// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v8.0", FrameworkDisplayName = ".NET 8.0")]

```

### MotoRent.UnitTests.AssemblyInfo.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.UnitTests\obj\Debug\net8.0\MotoRent.UnitTests.AssemblyInfo.cs

Descricao:

```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;

[assembly: System.Reflection.AssemblyCompanyAttribute("MotoRent.UnitTests")]
[assembly: System.Reflection.AssemblyConfigurationAttribute("Debug")]
[assembly: System.Reflection.AssemblyFileVersionAttribute("1.0.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersionAttribute("1.0.0+64cd47327392c08c41903fa05e6d63212e80a7b1")]
[assembly: System.Reflection.AssemblyProductAttribute("MotoRent.UnitTests")]
[assembly: System.Reflection.AssemblyTitleAttribute("MotoRent.UnitTests")]
[assembly: System.Reflection.AssemblyVersionAttribute("1.0.0.0")]

// Generated by the MSBuild WriteCodeFragment class.


```

### MotoRent.UnitTests.GlobalUsings.g.cs
Path: D:\TestesTrabalho\MotoRentAPI\tests\MotoRent.UnitTests\obj\Debug\net8.0\MotoRent.UnitTests.GlobalUsings.g.cs

Descricao:

```csharp
// <auto-generated/>
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;
global using global::Xunit;

```

