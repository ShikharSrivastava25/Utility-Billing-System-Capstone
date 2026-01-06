using UtilityBillingSystem.Models.Dto.Tariff;

namespace UtilityBillingSystem.Services.Interfaces
{
    public interface ITariffService
    {
        Task<IEnumerable<TariffDto>> GetTariffsAsync();
        Task<TariffDto> GetTariffByIdAsync(string id);
        Task<TariffDto> CreateTariffAsync(TariffDto dto, string currentUserEmail);
        Task<TariffDto> UpdateTariffAsync(string id, TariffDto dto, string currentUserEmail);
        Task DeleteTariffAsync(string id, string currentUserEmail);
    }
}

