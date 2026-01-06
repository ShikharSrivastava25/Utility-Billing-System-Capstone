using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.UtilityType;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Services
{
    public class UtilityTypeService : IUtilityTypeService
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IMapper _mapper;

        public UtilityTypeService(AppDbContext context, IAuditLogService auditLogService, IMapper mapper)
        {
            _context = context;
            _auditLogService = auditLogService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UtilityTypeDto>> GetUtilityTypesAsync()
        {
            var utilityTypes = await _context.UtilityTypes
                .OrderBy(u => u.Name.ToLower())
                .ToListAsync();
            return _mapper.Map<IEnumerable<UtilityTypeDto>>(utilityTypes);
        }

        public async Task<IEnumerable<UtilityTypeDto>> GetUtilityTypesForUserAsync(string userId)
        {
            var utilityTypeIds = await _context.Connections
                .Where(c => c.UserId == userId)
                .Select(c => c.UtilityTypeId)
                .Distinct()
                .ToListAsync();

            // Get utility types that the user has connections for, and only return enabled ones
            var utilityTypes = await _context.UtilityTypes
                .Where(u => utilityTypeIds.Contains(u.Id) && u.Status == "Enabled")
                .ToListAsync();
            return _mapper.Map<IEnumerable<UtilityTypeDto>>(utilityTypes);
        }

        public async Task<UtilityTypeDto> GetUtilityTypeByIdAsync(string id)
        {
            var utilityType = await _context.UtilityTypes.FindAsync(id);
            if (utilityType == null)
                throw new KeyNotFoundException("Utility type not found");

            return _mapper.Map<UtilityTypeDto>(utilityType);
        }

        public async Task<UtilityTypeDto> CreateUtilityTypeAsync(UtilityTypeDto dto, string currentUserEmail)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required");

            // Validate billing cycle if assigned
            if (!string.IsNullOrWhiteSpace(dto.BillingCycleId))
            {
                var billingCycle = await _context.BillingCycles.FindAsync(dto.BillingCycleId);
                if (billingCycle == null)
                    throw new KeyNotFoundException("Billing cycle not found");
                
                if (!billingCycle.IsActive)
                    throw new InvalidOperationException("Cannot assign an inactive billing cycle to a utility. Please activate the billing cycle first.");
            }

            var utilityType = new UtilityType
            {
                Name = dto.Name,
                Description = dto.Description ?? string.Empty,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Enabled" : dto.Status,
                BillingCycleId = string.IsNullOrWhiteSpace(dto.BillingCycleId) ? null : dto.BillingCycleId
            };

            _context.UtilityTypes.Add(utilityType);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("UTILITY_CREATE", $"Created new utility '{utilityType.Name}'.", currentUserEmail);

            return _mapper.Map<UtilityTypeDto>(utilityType);
        }

        public async Task<UtilityTypeDto> UpdateUtilityTypeAsync(string id, UtilityTypeDto dto, string currentUserEmail)
        {
            var utilityType = await _context.UtilityTypes
                .Include(u => u.Connections)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (utilityType == null)
                throw new KeyNotFoundException("Utility type not found");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required");

            // Check if trying to disable utility type that has active connections
            var newStatus = string.IsNullOrWhiteSpace(dto.Status) ? "Enabled" : dto.Status;
            if (newStatus == "Disabled" && utilityType.Connections.Any(c => c.Status == "Active"))
                throw new InvalidOperationException("Cannot disable utility type with active connections. Please deactivate or remove all active connections first.");

            // Check if billing cycle is being removed and utility has active connections
            var billingCycleRemoved = !string.IsNullOrWhiteSpace(utilityType.BillingCycleId) && string.IsNullOrWhiteSpace(dto.BillingCycleId);
            if (billingCycleRemoved && utilityType.Connections.Any(c => c.Status == "Active"))
                throw new InvalidOperationException("Cannot remove billing cycle from utility type with active connections. Please deactivate or remove all active connections first.");

            // Validate billing cycle if being assigned or changed
            if (!string.IsNullOrWhiteSpace(dto.BillingCycleId))
            {
                var billingCycle = await _context.BillingCycles.FindAsync(dto.BillingCycleId);
                if (billingCycle == null)
                    throw new KeyNotFoundException("Billing cycle not found");
                
                if (!billingCycle.IsActive)
                    throw new InvalidOperationException("Cannot assign an inactive billing cycle to a utility. Please activate the billing cycle first.");
            }

            utilityType.Name = dto.Name;
            utilityType.Description = dto.Description ?? string.Empty;
            utilityType.Status = newStatus;
            utilityType.BillingCycleId = string.IsNullOrWhiteSpace(dto.BillingCycleId) ? null : dto.BillingCycleId;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("UTILITY_UPDATE", $"Updated utility '{utilityType.Name}'. New status: {utilityType.Status}.", currentUserEmail);

            return _mapper.Map<UtilityTypeDto>(utilityType);
        }

        public async Task DeleteUtilityTypeAsync(string id, string currentUserEmail)
        {
            var utilityType = await _context.UtilityTypes
                .Include(u => u.Connections)
                .Include(u => u.Tariffs)
                .Include(u => u.UtilityRequests)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (utilityType == null)
                throw new KeyNotFoundException("Utility type not found");

            // Check if utility type has any connections (active or inactive)
            if (utilityType.Connections.Any())
                throw new InvalidOperationException("Cannot delete utility type with existing connections. Please remove all connections first.");

            // Check if utility type has any tariffs (active or inactive)
            if (utilityType.Tariffs.Any())
                throw new InvalidOperationException("Cannot delete utility type with existing tariffs. Please remove all tariffs first.");

            // Check if utility type has any utility requests
            if (utilityType.UtilityRequests.Any())
                throw new InvalidOperationException("Cannot delete utility type with existing utility requests. Please handle utility requests first.");

            var utilityName = utilityType.Name;
            _context.UtilityTypes.Remove(utilityType);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("UTILITY_DELETE", $"Deleted utility type '{utilityName}'.", currentUserEmail);
        }
    }
}

