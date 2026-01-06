using Moq;
using UtilityBillingSystem.Models.Dto.Connection;
using UtilityBillingSystem.Models.Dto.Payment;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Services.Interfaces;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class ReportServiceTests : BaseServiceTest
    {
        private readonly Mock<IPaymentService> _mockPaymentService;
        private readonly Mock<IConnectionService> _mockConnectionService;
        private readonly Mock<IBillService> _mockBillService;
        private readonly ReportService _service;

        public ReportServiceTests() : base()
        {
            _mockPaymentService = new Mock<IPaymentService>();
            _mockConnectionService = new Mock<IConnectionService>();
            _mockBillService = new Mock<IBillService>();
            _service = new ReportService(Context, _mockPaymentService.Object, _mockConnectionService.Object, _mockBillService.Object);
        }

    }
}

