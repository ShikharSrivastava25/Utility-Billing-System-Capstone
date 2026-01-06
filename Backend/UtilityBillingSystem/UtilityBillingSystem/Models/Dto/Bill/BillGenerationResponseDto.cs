namespace UtilityBillingSystem.Models.Dto.Bill
{
    public class BillGenerationResponseDto
    {
        public int GeneratedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> GeneratedBillIds { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }
}

