namespace UtilityBillingSystem.Models.Dto.AccountOfficer
{
    public class AccountOfficerDashboardDto
    {
        public decimal TotalRevenue { get; set; }
        public int UnpaidBillsCount { get; set; }
        public decimal OutstandingDues { get; set; }
        public decimal TotalConsumption { get; set; }
    }
}

