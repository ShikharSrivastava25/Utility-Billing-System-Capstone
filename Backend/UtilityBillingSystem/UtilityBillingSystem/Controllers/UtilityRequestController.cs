using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityBillingSystem.Models.Dto.UtilityRequest;
using UtilityBillingSystem.Models.Dto.Connection;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UtilityRequestController : BaseController
    {
        private readonly IUtilityRequestService _utilityRequestService;

        public UtilityRequestController(IUtilityRequestService utilityRequestService)
        {
            _utilityRequestService = utilityRequestService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Billing Officer")]
        public async Task<ActionResult<IEnumerable<UtilityRequestDto>>> GetRequests()
        {
            var requests = await _utilityRequestService.GetRequestsAsync();
            return Ok(requests);
        }

        [HttpGet("my-requests")]
        [Authorize(Roles = "Consumer")]
        public async Task<ActionResult<IEnumerable<UtilityRequestDto>>> GetMyRequests()
        {
            var requests = await _utilityRequestService.GetRequestsForUserAsync(CurrentUserId);
            return Ok(requests);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Billing Officer,Consumer")]
        public async Task<ActionResult<UtilityRequestDto>> GetRequest(string id)
        {
            var request = await _utilityRequestService.GetRequestByIdAsync(id);
            
            // Ensure consumers can only view their own requests
            var isConsumer = User.IsInRole("Consumer");
            
            if (isConsumer && request.UserId != CurrentUserId)
            {
                return StatusCode(403, new { error = new { message = "You can only view your own requests." } });
            }
            
            return Ok(request);
        }

        [HttpPost]
        [Authorize(Roles = "Consumer,Admin")]
        public async Task<ActionResult<UtilityRequestDto>> CreateRequest([FromBody] UtilityRequestDto dto)
        {
            var isAdmin = User.IsInRole("Admin");
            var request = await _utilityRequestService.CreateRequestAsync(dto, CurrentUserId, isAdmin);
            return Ok(request);
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ConnectionDto>> ApproveRequest(string id, [FromBody] ApproveRequestDto dto)
        {
            var connection = await _utilityRequestService.ApproveRequestAsync(id, dto, CurrentUserEmail);
            return Ok(connection);
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UtilityRequestDto>> RejectRequest(string id)
        {
            var request = await _utilityRequestService.RejectRequestAsync(id, CurrentUserEmail);
            return Ok(request);
        }
    }
}


