using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityBillingSystem.Models.Dto.BillingCycle;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BillingCycleController : BaseController
    {
        private readonly IBillingCycleService _billingCycleService;

        public BillingCycleController(IBillingCycleService billingCycleService)
        {
            _billingCycleService = billingCycleService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Billing Officer")]
        public async Task<ActionResult<IEnumerable<BillingCycleDto>>> GetBillingCycles()
        {
            var cycles = await _billingCycleService.GetBillingCyclesAsync();
            return Ok(cycles);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Billing Officer")]
        public async Task<ActionResult<BillingCycleDto>> GetBillingCycle(string id)
        {
            var cycle = await _billingCycleService.GetBillingCycleByIdAsync(id);
            return Ok(cycle);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BillingCycleDto>> CreateBillingCycle([FromBody] BillingCycleDto dto)
        {
            var cycle = await _billingCycleService.CreateBillingCycleAsync(dto, CurrentUserEmail);
            return Ok(cycle);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BillingCycleDto>> UpdateBillingCycle(string id, [FromBody] BillingCycleDto dto)
        {
            var cycle = await _billingCycleService.UpdateBillingCycleAsync(id, dto, CurrentUserEmail);
            return Ok(cycle);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBillingCycle(string id)
        {
            await _billingCycleService.DeleteBillingCycleAsync(id, CurrentUserEmail);
            return Ok(new { message = "Billing cycle deleted successfully" });
        }
    }
}


