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