using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityBillingSystem.Models.Dto.Connection;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConnectionController : BaseController
    {
        private readonly IConnectionService _connectionService;

        public ConnectionController(IConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ConnectionDto>>> GetConnections()
        {
            var connections = await _connectionService.GetConnectionsAsync();
            return Ok(connections);
        }

        [HttpGet("my-connections")]
        [Authorize(Roles = "Consumer,Admin")]
        public async Task<ActionResult<IEnumerable<ConnectionDto>>> GetMyConnections()
        {
            var connections = await _connectionService.GetConnectionsForUserAsync(CurrentUserId);
            return Ok(connections);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Billing Officer,Account Officer,Consumer")]
        public async Task<ActionResult<ConnectionDto>> GetConnection(string id)
        {
            var isConsumer = User.IsInRole("Consumer");
            var connection = await _connectionService.GetConnectionByIdAsync(id, CurrentUserId, isConsumer);
            return Ok(connection);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ConnectionDto>> CreateConnection([FromBody] ConnectionDto dto)
        {
            var connection = await _connectionService.CreateConnectionAsync(dto, CurrentUserEmail);
            return Ok(connection);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ConnectionDto>> UpdateConnection(string id, [FromBody] ConnectionDto dto)
        {
            var connection = await _connectionService.UpdateConnectionAsync(id, dto, CurrentUserEmail);
            return Ok(connection);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConnection(string id)
        {
            await _connectionService.DeleteConnectionAsync(id, CurrentUserEmail);
            return Ok(new { message = "Connection deleted successfully" });
        }
    }
}


