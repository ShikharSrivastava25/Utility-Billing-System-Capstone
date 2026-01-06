using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityBillingSystem.Models.Dto.Tariff;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TariffController : BaseController
    {
        private readonly ITariffService _tariffService;

        public TariffController(ITariffService tariffService)
        {
            _tariffService = tariffService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Billing Officer")]
        public async Task<ActionResult<IEnumerable<TariffDto>>> GetTariffs()
        {
            var tariffs = await _tariffService.GetTariffsAsync();
            return Ok(tariffs);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Billing Officer")]
        public async Task<ActionResult<TariffDto>> GetTariff(string id)
        {
            var tariff = await _tariffService.GetTariffByIdAsync(id);
            return Ok(tariff);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TariffDto>> CreateTariff([FromBody] TariffDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { error = new { message = "Request body is required" } });
            }

            var tariff = await _tariffService.CreateTariffAsync(dto, CurrentUserEmail);
            return Ok(tariff);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TariffDto>> UpdateTariff(string id, [FromBody] TariffDto dto)
        {
            var tariff = await _tariffService.UpdateTariffAsync(id, dto, CurrentUserEmail);
            return Ok(tariff);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTariff(string id)
        {
            await _tariffService.DeleteTariffAsync(id, CurrentUserEmail);
            return Ok(new { message = "Tariff deleted successfully" });
        }
    }
}


