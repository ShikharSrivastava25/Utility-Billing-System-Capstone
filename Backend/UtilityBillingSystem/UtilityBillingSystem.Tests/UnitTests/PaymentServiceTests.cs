using UtilityBillingSystem.Models.Dto.Payment;
using UtilityBillingSystem.Services;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class PaymentServiceTests : BaseServiceTest
    {
        private readonly PaymentService _service;

        public PaymentServiceTests() : base()
        {
            _service = new PaymentService(Context, MockAuditLogService.Object, Mapper);
        }

        [Fact]
        public async Task RecordPaymentAsync_WithValidBill_RecordsPayment()
        {
            // Arrange
            var connection = await CreateFullConnectionAsync();
            var bill = await CreateBillAsync(connection.Id, 100m, 500m, 90m, 590m, status: "Generated");

            var dto = new CreatePaymentDto
            {
                PaymentMethod = "Cash",
                ReceiptNumber = "R001"
            };

            // Act
            var result = await _service.RecordPaymentAsync(bill.Id, dto, connection.UserId, "test@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(bill.Id, result.BillId);
            Assert.Equal(590m, result.Amount);
            Assert.Equal("Cash", result.PaymentMethod);
            Assert.Equal("R001", result.ReceiptNumber);
            Assert.Equal("Completed", result.Status);

            // Verify bill status updated
            await Context.Entry(bill).ReloadAsync();
            Assert.Equal("Paid", bill.Status);
        }

        [Fact]
        public async Task RecordPaymentAsync_WithAlreadyPaidBill_ThrowsInvalidOperationException()
        {
            // Arrange
            var connection = await CreateFullConnectionAsync();
            var bill = await CreateBillAsync(connection.Id, 100m, 500m, 90m, 590m, status: "Paid");

            var dto = new CreatePaymentDto
            {
                PaymentMethod = "Cash",
                ReceiptNumber = "R001"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RecordPaymentAsync(bill.Id, dto, connection.UserId, "test@example.com"));
            Assert.Contains("Bill is already paid", ex.Message);
        }

        [Fact]
        public async Task RecordPaymentAsync_WithUnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var user1 = await CreateUserAsync();
            var user2 = await CreateUserAsync();
            var connection = await CreateFullConnectionAsync(userId: user1.Id);
            var bill = await CreateBillAsync(connection.Id, 100m, 500m, 90m, 590m, status: "Generated");

            var dto = new CreatePaymentDto
            {
                PaymentMethod = "Cash",
                ReceiptNumber = "R001"
            };

            // Act & Assert - user2 tries to pay user1's bill
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.RecordPaymentAsync(bill.Id, dto, user2.Id, "test@example.com"));
        }

    }
}

