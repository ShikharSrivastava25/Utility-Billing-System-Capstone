using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityBillingSystem.Models.Dto.MeterReading;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Billing Officer")]
    public class MeterReadingController : BaseController
    {
        private readonly IMeterReadingService _meterReadingService;

        public MeterReadingController(IMeterReadingService meterReadingService)
        {
            _meterReadingService = meterReadingService;
        }

        [HttpGet("connections")]
        public async Task<ActionResult<IEnumerable<ConnectionForReadingDto>>> GetConnectionsNeedingReadings()
        {
            var connections = await _meterReadingService.GetConnectionsNeedingReadingsAsync();
            return Ok(connections);
        }

        [HttpGet("previous/{connectionId}")]
        public async Task<ActionResult<decimal?>> GetPreviousReading(string connectionId)
        {
            var previousReading = await _meterReadingService.GetPreviousReadingAsync(connectionId);
            return Ok(previousReading ?? 0);
        }

        [HttpPost]
        public async Task<ActionResult<MeterReadingResponseDto>> CreateMeterReading([FromBody] MeterReadingRequestDto dto)
        {
            var reading = await _meterReadingService.CreateMeterReadingAsync(dto, CurrentUserEmail);
            
            if (reading == null)
                return BadRequest(new { error = new { message = "Failed to create meter reading" } });

            return Ok(reading);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MeterReadingResponseDto>>> GetMeterReadingHistory(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? utilityTypeId = null,
            string? consumerName = null,
            string? status = null,
            int page = 1,
            int pageSize = 50)
        {
            var readings = await _meterReadingService.GetMeterReadingHistoryAsync(
                startDate, endDate, utilityTypeId, consumerName, status, page, pageSize);
            return Ok(readings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MeterReadingResponseDto>> GetMeterReading(string id)
        {
            var reading = await _meterReadingService.GetMeterReadingByIdAsync(id);
            if (reading == null)
                return NotFound(new { error = new { message = "Meter reading not found" } });

            return Ok(reading);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<MeterReadingResponseDto>> UpdateMeterReading(string id, [FromBody] UpdateMeterReadingRequestDto dto)
        {
            var reading = await _meterReadingService.UpdateMeterReadingAsync(id, dto.CurrentReading, CurrentUserEmail);
            
            if (reading == null)
                return NotFound(new { error = new { message = "Meter reading not found" } });

            return Ok(reading);
        }
    }
}


