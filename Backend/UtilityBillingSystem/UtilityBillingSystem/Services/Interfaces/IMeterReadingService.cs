using UtilityBillingSystem.Models.Dto.MeterReading;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface IMeterReadingService
    {
        Task<IEnumerable<ConnectionForReadingDto>> GetConnectionsNeedingReadingsAsync();
        Task<decimal?> GetPreviousReadingAsync(string connectionId);
        Task<MeterReadingResponseDto> CreateMeterReadingAsync(MeterReadingRequestDto dto, string userEmail);
        Task<IEnumerable<MeterReadingResponseDto>> GetMeterReadingHistoryAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? utilityTypeId = null,
            string? consumerName = null,
            string? status = null,
            int page = 1,
            int pageSize = 50);
        Task<MeterReadingResponseDto?> GetMeterReadingByIdAsync(string id);
        Task<MeterReadingResponseDto> UpdateMeterReadingAsync(string id, decimal currentReading, string userEmail);
    }
}

