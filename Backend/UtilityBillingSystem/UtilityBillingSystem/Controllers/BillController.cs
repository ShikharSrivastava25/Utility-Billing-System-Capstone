using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityBillingSystem.Models.Dto.Bill;
using UtilityBillingSystem.Models.Dto.Payment;
using UtilityBillingSystem.Models.Dto.AccountOfficer;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BillController : BaseController
    {
        private readonly IBillService _billService;
        private readonly IPaymentService _paymentService;
        private readonly IAccountOfficerService _accountOfficerService;

        public BillController(IBillService billService, IPaymentService paymentService, IAccountOfficerService accountOfficerService)
        {
            _billService = billService;
            _paymentService = paymentService;
            _accountOfficerService = accountOfficerService;
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Billing Officer")]
        public async Task<ActionResult<IEnumerable<PendingBillDto>>> GetPendingBills()
        {
            var pendingBills = await _billService.GetPendingBillsAsync();
            return Ok(pendingBills);
        }

        [HttpPost("generate")]
        [Authorize(Roles = "Billing Officer")]
        public async Task<ActionResult<BillDto>> GenerateBill([FromBody] GenerateBillRequestDto dto)
        {
            var bill = await _billService.GenerateBillAsync(dto.ReadingId, CurrentUserEmail);
            return Ok(bill);
        }

        [HttpPost("generate/batch")]
        [Authorize(Roles = "Billing Officer")]
        public async Task<ActionResult<BillGenerationResponseDto>> GenerateBillsBatch([FromBody] BillGenerationRequestDto dto)
        {
            var result = await _billService.GenerateBillsBatchAsync(dto.ReadingIds, CurrentUserEmail);
            return Ok(result);
        }

        [HttpGet("connection/{connectionId}")]
        [Authorize(Roles = "Billing Officer")]
        public async Task<ActionResult<IEnumerable<BillDetailDto>>> GetBillsByConnection(string connectionId)
        {
            var bills = await _billService.GetBillsByConnectionAsync(connectionId);
            return Ok(bills);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Billing Officer,Account Officer")]
        public async Task<ActionResult<BillDetailDto>> GetBill(string id)
        {
            var bill = await _billService.GetBillByIdAsync(id);
            if (bill == null)
                return NotFound(new { error = new { message = "Bill not found" } });

            return Ok(bill);
        }

        // Consumer endpoints
        [HttpGet("my-bills")]
        [Authorize(Roles = "Consumer")]
        public async Task<ActionResult<IEnumerable<BillDetailDto>>> GetMyBills()
        {
            var bills = await _billService.GetBillsForUserAsync(CurrentUserId);
            return Ok(bills);
        }

        [HttpGet("my-bills/{id}")]
        [Authorize(Roles = "Consumer")]
        public async Task<ActionResult<BillDetailDto>> GetMyBill(string id)
        {
            var bill = await _billService.GetBillForUserByIdAsync(id, CurrentUserId);
            if (bill == null)
                return NotFound(new { error = new { message = "Bill not found" } });

            return Ok(bill);
        }

        [HttpPost("{id}/pay")]
        [Authorize(Roles = "Consumer")]
        public async Task<ActionResult<PaymentDto>> PayBill(string id, [FromBody] CreatePaymentDto dto)
        {
            var payment = await _paymentService.RecordPaymentAsync(id, dto, CurrentUserId, CurrentUserEmail);
            return Ok(payment);
        }

        // Account Officer Endpoints
        [HttpGet("outstanding")]
        [Authorize(Roles = "Account Officer")]
        public async Task<ActionResult<PagedResult<OutstandingBillDto>>> GetOutstandingBills(
            string? statusFilter = null,
            int page = 1,
            int pageSize = 25)
        {
            var result = await _accountOfficerService.GetOutstandingBillsAsync(statusFilter, page, pageSize);
            return Ok(result);
        }

        [HttpGet("consumers/summary")]
        [Authorize(Roles = "Account Officer")]
        public async Task<ActionResult<PagedResult<ConsumerBillingSummaryDto>>> GetConsumerBillingSummary(
            int page = 1,
            int pageSize = 25)
        {
            var result = await _accountOfficerService.GetConsumerBillingSummaryAsync(page, pageSize);
            return Ok(result);
        }
    }
}


