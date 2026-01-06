namespace UtilityBillingSystem.Models.Dto.Consumer
{
    public class ConsumerDashboardResponse
    {
        public decimal OutstandingBalance { get; set; }
        public decimal MonthlySpending { get; set; }
        public int ActiveConnections { get; set; }
        public int DueBillsCount { get; set; }
        public IEnumerable<ConsumptionPoint> ConsumptionTrend { get; set; } = Enumerable.Empty<ConsumptionPoint>();
    }
}

