namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogActionAsync(string action, string details, string performedBy);
    }
}


