namespace UtilityBillingSystem.Models.Dto.BillingCycle
{
    public class BillingCycleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int GenerationDay { get; set; }
        public int DueDateOffset { get; set; }
        public int GracePeriod { get; set; }
        public bool IsActive { get; set; }
    }
}

