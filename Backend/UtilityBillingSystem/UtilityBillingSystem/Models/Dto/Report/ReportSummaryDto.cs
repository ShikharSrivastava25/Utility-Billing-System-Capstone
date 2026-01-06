namespace UtilityBillingSystem.Models.Dto.Report
{
    public class ReportSummaryDto
    {
        public int ActiveBillingOfficers { get; set; }
        public int ActiveAccountOfficers { get; set; }
        public int TotalConsumers { get; set; }
        public int PendingUtilityRequests { get; set; }
    }
}

