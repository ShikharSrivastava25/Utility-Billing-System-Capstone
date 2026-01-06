using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Dto.AuditLog;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditLogController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(al => al.Timestamp)
                .Select(al => new AuditLogDto
                {
                    Timestamp = al.Timestamp,
                    Action = al.Action,
                    Details = al.Details,
                    PerformedBy = al.PerformedBy
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}


