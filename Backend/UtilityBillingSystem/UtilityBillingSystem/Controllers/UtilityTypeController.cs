using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Models.Dto.UtilityType;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UtilityTypeController : BaseController
    {
        private readonly IUtilityTypeService _utilityTypeService;

        public UtilityTypeController(IUtilityTypeService utilityTypeService)
        {
            _utilityTypeService = utilityTypeService;
        }

        [HttpGet]
        [Authorize] // Allow all authenticated users (Admin, Billing Officer, Account Officer, Consumer)
        public async Task<ActionResult<IEnumerable<UtilityTypeDto>>> GetUtilityTypes()
        {
            var utilityTypes = await _utilityTypeService.GetUtilityTypesAsync();
            return Ok(utilityTypes);
        }

        [HttpGet("my-utilities")]
        [Authorize(Roles = "Consumer,Admin")]
        public async Task<ActionResult<IEnumerable<UtilityTypeDto>>> GetMyUtilityTypes()
        {
            var utilityTypes = await _utilityTypeService.GetUtilityTypesForUserAsync(CurrentUserId);
            return Ok(utilityTypes);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Billing Officer")]
        public async Task<ActionResult<UtilityTypeDto>> GetUtilityType(string id)
        {
            var utilityType = await _utilityTypeService.GetUtilityTypeByIdAsync(id);
            return Ok(utilityType);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UtilityTypeDto>> CreateUtilityType([FromBody] UtilityTypeDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { error = new { message = "Request body is required" } });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = new { message = "Invalid model state", details = ModelState } });
            }

            var utilityType = await _utilityTypeService.CreateUtilityTypeAsync(dto, CurrentUserEmail);
            return Ok(utilityType);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UtilityTypeDto>> UpdateUtilityType(string id, [FromBody] UtilityTypeDto dto)
        {
            var utilityType = await _utilityTypeService.UpdateUtilityTypeAsync(id, dto, CurrentUserEmail);
            return Ok(utilityType);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUtilityType(string id)
        {
            await _utilityTypeService.DeleteUtilityTypeAsync(id, CurrentUserEmail);
            return Ok(new { message = "Utility type deleted successfully" });
        }
    }
}


