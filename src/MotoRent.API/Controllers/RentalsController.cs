
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