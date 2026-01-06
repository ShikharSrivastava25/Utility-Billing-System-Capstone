using UtilityBillingSystem.Services;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class BillCalculationServiceTests
    {
        private readonly BillCalculationService _service;

        public BillCalculationServiceTests()
        {
            _service = new BillCalculationService();
        }

        [Fact]
        public void CalculateBillAmount_WithValidInputs_ReturnsCorrectTotals()
        {
            // Arrange
            var consumption = 750m;
            var baseRate = 6.0m;
            var fixedCharge = 150.0m;
            var taxPercentage = 18.0m;

            // Act
            var result = _service.CalculateBillAmount(consumption, baseRate, fixedCharge, taxPercentage);

            // Assert
            Assert.Equal(750m, result.Consumption);
            Assert.Equal(6.0m, result.BaseRate);
            Assert.Equal(150.0m, result.FixedCharge);
            Assert.Equal(4650m, result.BaseAmount); // (750 * 6) + 150 = 4650
            Assert.Equal(18.0m, result.TaxPercentage);
            Assert.Equal(837m, result.TaxAmount); // 4650 * 0.18 = 837
            Assert.Equal(5487m, result.TotalAmount); // 4650 + 837 = 5487
        }

        [Fact]
        public void CalculateBillAmount_WithZeroConsumption_ReturnsFixedChargeOnly()
        {
            // Arrange
            var consumption = 0m;
            var baseRate = 5.0m;
            var fixedCharge = 100.0m;
            var taxPercentage = 18.0m;

            // Act
            var result = _service.CalculateBillAmount(consumption, baseRate, fixedCharge, taxPercentage);

            // Assert - Even with zero consumption, fixed charge applies
            Assert.Equal(100m, result.BaseAmount);
            Assert.Equal(18m, result.TaxAmount);
            Assert.Equal(118m, result.TotalAmount);
        }

        [Fact]
        public void CalculateBillAmount_WithNegativeConsumption_ThrowsArgumentException()
        {
            // Arrange
            var consumption = -100m;
            var baseRate = 5.0m;
            var fixedCharge = 100.0m;
            var taxPercentage = 18.0m;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CalculateBillAmount(consumption, baseRate, fixedCharge, taxPercentage));
            Assert.Contains("Consumption cannot be negative", ex.Message);
        }

        [Fact]
        public void CalculateBillAmount_WithNegativeBaseRate_ThrowsArgumentException()
        {
            // Arrange
            var consumption = 100m;
            var baseRate = -5.0m;
            var fixedCharge = 100.0m;
            var taxPercentage = 18.0m;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CalculateBillAmount(consumption, baseRate, fixedCharge, taxPercentage));
            Assert.Contains("Base rate cannot be negative", ex.Message);
        }

        [Fact]
        public void CalculateBillAmount_WithNegativeFixedCharge_ThrowsArgumentException()
        {
            // Arrange
            var consumption = 100m;
            var baseRate = 5.0m;
            var fixedCharge = -100.0m;
            var taxPercentage = 18.0m;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CalculateBillAmount(consumption, baseRate, fixedCharge, taxPercentage));
            Assert.Contains("Fixed charge cannot be negative", ex.Message);
        }

        [Fact]
        public void CalculateBillAmount_WithInvalidTaxPercentage_ThrowsArgumentException()
        {
            // Arrange
            var consumption = 100m;
            var baseRate = 5.0m;
            var fixedCharge = 100.0m;
            var taxPercentage = 150.0m; // Invalid: > 100

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CalculateBillAmount(consumption, baseRate, fixedCharge, taxPercentage));
            Assert.Contains("Tax percentage must be between 0 and 100", ex.Message);
        }

        [Fact]
        public void CalculatePenalty_WithLowFixedCharge_UsesMinimumOneRupeePerDay()
        {
            // Arrange
            var fixedCharge = 50m;
            var daysOverdue = 10;

            // Act
            var result = _service.CalculatePenalty(fixedCharge, daysOverdue);

            // Assert - Should use minimum ₹1 per day
            Assert.Equal(10m, result); // 10 days * ₹1 = ₹10
        }

        [Fact]
        public void CalculatePenalty_WithHighFixedCharge_UsesPercentageOfFixedCharge()
        {
            // Arrange
            var fixedCharge = 500m; // High fixed charge
            var daysOverdue = 5;

            // Act
            var result = _service.CalculatePenalty(fixedCharge, daysOverdue);

            // Assert - Should use 1% of fixed charge per day (₹5 per day)
            Assert.Equal(25m, result); // 5 days * ₹5 = ₹25
        }

        [Fact]
        public void CalculatePenalty_WithZeroDaysOverdue_ReturnsZero()
        {
            // Arrange
            var fixedCharge = 500m;
            var daysOverdue = 0;

            // Act
            var result = _service.CalculatePenalty(fixedCharge, daysOverdue);

            // Assert
            Assert.Equal(0m, result);
        }

        [Fact]
        public void CalculatePenalty_WithNegativeFixedCharge_ThrowsArgumentException()
        {
            // Arrange
            var fixedCharge = -100m;
            var daysOverdue = 10;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CalculatePenalty(fixedCharge, daysOverdue));
            Assert.Contains("Fixed charge cannot be negative", ex.Message);
        }

        [Fact]
        public void CalculatePenalty_WithNegativeDaysOverdue_ThrowsArgumentException()
        {
            // Arrange
            var fixedCharge = 500m;
            var daysOverdue = -5;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CalculatePenalty(fixedCharge, daysOverdue));
            Assert.Contains("Days overdue cannot be negative", ex.Message);
        }

        [Fact]
        public void DetermineBillStatus_WithGeneratedStatus_ReturnsGenerated()
        {
            // Arrange
            var dueDate = DateTime.UtcNow.AddDays(5); // Due in future
            var now = DateTime.UtcNow;
            var gracePeriod = 7;

            // Act
            var result = _service.DetermineBillStatus(dueDate, now, gracePeriod);

            // Assert
            Assert.Equal("Generated", result);
        }

        [Fact]
        public void DetermineBillStatus_WithDueStatus_ReturnsDue()
        {
            // Arrange
            var dueDate = DateTime.UtcNow.AddDays(-2); // Due 2 days ago
            var now = DateTime.UtcNow;
            var gracePeriod = 7;

            // Act
            var result = _service.DetermineBillStatus(dueDate, now, gracePeriod);

            // Assert
            Assert.Equal("Due", result);
        }

        [Fact]
        public void DetermineBillStatus_WithOverdueStatus_ReturnsOverdue()
        {
            // Arrange
            var dueDate = DateTime.UtcNow.AddDays(-10); // Due 10 days ago
            var now = DateTime.UtcNow;
            var gracePeriod = 7; // Grace period expired

            // Act
            var result = _service.DetermineBillStatus(dueDate, now, gracePeriod);

            // Assert
            Assert.Equal("Overdue", result);
        }

        [Fact]
        public void DetermineBillStatus_WithNegativeGracePeriod_ThrowsArgumentException()
        {
            // Arrange
            var dueDate = DateTime.UtcNow;
            var now = DateTime.UtcNow;
            var gracePeriod = -5;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.DetermineBillStatus(dueDate, now, gracePeriod));
            Assert.Contains("Grace period cannot be negative", ex.Message);
        }

        [Fact]
        public void CanGenerateBill_WithReadyForBillingStatus_ReturnsTrue()
        {
            // Act
            var result = _service.CanGenerateBill("ReadyForBilling");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanGenerateBill_WithBilledStatus_ReturnsFalse()
        {
            // Act
            var result = _service.CanGenerateBill("Billed");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanGenerateBill_WithPendingStatus_ReturnsFalse()
        {
            // Act
            var result = _service.CanGenerateBill("Pending");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanGenerateBill_WithNullStatus_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CanGenerateBill(null!));
            Assert.Contains("Reading status cannot be null or empty", ex.Message);
        }

        [Fact]
        public void ValidateAndCalculateConsumption_WithValidReadings_ReturnsCorrectConsumption()
        {
            // Arrange
            var currentReading = 1500m;
            var previousReading = 1000m;

            // Act
            var result = _service.ValidateAndCalculateConsumption(currentReading, previousReading);

            // Assert
            Assert.Equal(500m, result);
        }

        [Fact]
        public void ValidateAndCalculateConsumption_WithFirstReading_ReturnsCurrentReading()
        {
            // Arrange
            var currentReading = 500m;
            var previousReading = 0m;

            // Act
            var result = _service.ValidateAndCalculateConsumption(currentReading, previousReading);

            // Assert
            Assert.Equal(500m, result);
        }

        [Fact]
        public void ValidateAndCalculateConsumption_WithCurrentLessThanPrevious_ThrowsArgumentException()
        {
            // Arrange
            var currentReading = 1000m;
            var previousReading = 1500m;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.ValidateAndCalculateConsumption(currentReading, previousReading));
            Assert.Contains("Current reading cannot be less than previous reading", ex.Message);
        }

        [Fact]
        public void ValidateAndCalculateConsumption_WithNegativeCurrentReading_ThrowsArgumentException()
        {
            // Arrange
            var currentReading = -100m;
            var previousReading = 1000m;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.ValidateAndCalculateConsumption(currentReading, previousReading));
            Assert.Contains("Current reading cannot be negative", ex.Message);
        }

        [Fact]
        public void ValidateAndCalculateConsumption_WithNegativePreviousReading_ThrowsArgumentException()
        {
            // Arrange
            var currentReading = 1000m;
            var previousReading = -100m;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.ValidateAndCalculateConsumption(currentReading, previousReading));
            Assert.Contains("Previous reading cannot be negative", ex.Message);
        }

        [Fact]
        public void CalculateDueDate_WithValidInputs_ReturnsCorrectDueDate()
        {
            // Arrange
            var readingDate = new DateTime(2025, 3, 15);
            var dueDateOffset = 30;

            // Act
            var result = _service.CalculateDueDate(readingDate, dueDateOffset);

            // Assert
            Assert.Equal(new DateTime(2025, 4, 14), result);
        }

        [Fact]
        public void CalculateDueDate_WithZeroOffset_ReturnsSameDate()
        {
            // Arrange
            var readingDate = new DateTime(2025, 3, 15);
            var dueDateOffset = 0;

            // Act
            var result = _service.CalculateDueDate(readingDate, dueDateOffset);

            // Assert
            Assert.Equal(readingDate, result);
        }

        [Fact]
        public void CalculateDueDate_WithNegativeOffset_ThrowsArgumentException()
        {
            // Arrange
            var readingDate = new DateTime(2025, 3, 15);
            var dueDateOffset = -10;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CalculateDueDate(readingDate, dueDateOffset));
            Assert.Contains("Due date offset cannot be negative", ex.Message);
        }

        [Fact]
        public void CalculateDaysOverdue_WithNoOverdue_ReturnsZero()
        {
            // Arrange
            var dueDate = DateTime.UtcNow.AddDays(-2);
            var now = DateTime.UtcNow;
            var gracePeriod = 7; // Still within grace period

            // Act
            var result = _service.CalculateDaysOverdue(dueDate, now, gracePeriod);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateDaysOverdue_WithOverdue_ReturnsCorrectDays()
        {
            // Arrange
            var dueDate = DateTime.UtcNow.AddDays(-10);
            var now = DateTime.UtcNow;
            var gracePeriod = 7; // Overdue by 3 days (10 - 7 = 3)

            // Act
            var result = _service.CalculateDaysOverdue(dueDate, now, gracePeriod);

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void CalculateDaysOverdue_WithNegativeGracePeriod_ThrowsArgumentException()
        {
            // Arrange
            var dueDate = DateTime.UtcNow;
            var now = DateTime.UtcNow;
            var gracePeriod = -5;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CalculateDaysOverdue(dueDate, now, gracePeriod));
            Assert.Contains("Grace period cannot be negative", ex.Message);
        }
    }
}

