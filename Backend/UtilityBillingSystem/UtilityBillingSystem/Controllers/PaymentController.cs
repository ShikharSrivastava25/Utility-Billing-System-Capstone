using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityBillingSystem.Models.Dto.Payment;
using UtilityBillingSystem.Models.Dto.AccountOfficer;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : BaseController
    {
        private readonly IPaymentService _paymentService;
        private readonly IAccountOfficerService _accountOfficerService;

        public PaymentController(IPaymentService paymentService, IAccountOfficerService accountOfficerService)
        {
            _paymentService = paymentService;
            _accountOfficerService = accountOfficerService;
        }

        [HttpGet("my-payments")]
        [Authorize(Roles = "Consumer")]
        public async Task<ActionResult<IEnumerable<PaymentHistoryDto>>> GetMyPayments(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? utilityTypeId = null)
        {
            var payments = await _paymentService.GetPaymentHistoryForUserAsync(CurrentUserId, startDate, endDate, utilityTypeId);
            return Ok(payments);
        }

        [HttpGet("audit")]
        [Authorize(Roles = "Account Officer")]
        public async Task<ActionResult<PagedResult<PaymentAuditDto>>> GetPaymentAudit(
            int page = 1,
            int pageSize = 25)
        {
            var result = await _accountOfficerService.GetAllPaymentsAsync(page, pageSize);
            return Ok(result);
        }
    }
}

