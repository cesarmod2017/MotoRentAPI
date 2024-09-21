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