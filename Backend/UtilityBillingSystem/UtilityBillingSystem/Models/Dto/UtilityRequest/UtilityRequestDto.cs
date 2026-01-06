namespace UtilityBillingSystem.Models.Dto.UtilityRequest
{
    public class UtilityRequestDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UtilityTypeId { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime RequestDate { get; set; }
        public DateTime? DecisionDate { get; set; }
    }
}
