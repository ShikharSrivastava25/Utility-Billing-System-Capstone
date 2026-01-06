using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.Connection;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Services
{
    public class ConnectionService : IConnectionService
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IMapper _mapper;

        public ConnectionService(AppDbContext context, IAuditLogService auditLogService, IMapper mapper)
        {
            _context = context;
            _auditLogService = auditLogService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ConnectionDto>> GetConnectionsAsync()
        {
            var connections = await _context.Connections
                .Include(c => c.User)
                .Include(c => c.UtilityType)
                .Include(c => c.Tariff)
                .ToListAsync();

            var connectionDtos = _mapper.Map<IEnumerable<ConnectionDto>>(connections);
            return connectionDtos.OrderBy(c => c.UserName?.ToLowerInvariant() ?? "");
        }

        public async Task<IEnumerable<ConnectionDto>> GetConnectionsForUserAsync(string userId)
        {
            // Validate user exists and is not deleted
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Status == "Deleted")
                throw new KeyNotFoundException("User not found or has been deleted");

            var connections = await _context.Connections
                .Include(c => c.User)
                .Include(c => c.UtilityType)
                .Include(c => c.Tariff)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ConnectionDto>>(connections);
        }

        public async Task<ConnectionDto> GetConnectionByIdAsync(string id, string? userId = null, bool isConsumer = false)
        {
            var connection = await _context.Connections
                .Include(c => c.User)
                .Include(c => c.UtilityType)
                .Include(c => c.Tariff)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (connection == null)
                throw new KeyNotFoundException("Connection not found");

            // Validate ownership for consumers - they can only access their own connections
            if (isConsumer && !string.IsNullOrEmpty(userId) && connection.UserId != userId)
                throw new UnauthorizedAccessException("You do not have permission to access this connection");

            return _mapper.Map<ConnectionDto>(connection);
        }

        public async Task<ConnectionDto> CreateConnectionAsync(ConnectionDto dto, string currentUserEmail)
        {
            // Validate status
            if (dto.Status != "Active" && dto.Status != "Inactive")
                throw new ArgumentException("Status must be either 'Active' or 'Inactive'");

            // Validate meter number is not empty
            if (string.IsNullOrWhiteSpace(dto.MeterNumber))
                throw new ArgumentException("Meter number is required");

            // Check if meter number already exists
            if (await _context.Connections.AnyAsync(c => c.MeterNumber == dto.MeterNumber))
                throw new InvalidOperationException("Meter number already exists");

            // Check if user exists and is not deleted
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null || user.Status == "Deleted")
                throw new KeyNotFoundException("User not found or has been deleted");
            
            // Check if user is active (cannot create connection for inactive user)
            if (user.Status != "Active")
                throw new InvalidOperationException("Cannot create connection for an inactive user. Please activate the user first.");

            // Check if utility type exists and is enabled
            var utilityType = await _context.UtilityTypes.FindAsync(dto.UtilityTypeId);
            if (utilityType == null)
                throw new KeyNotFoundException("Utility type not found");
            
            if (utilityType.Status != "Enabled")
                throw new InvalidOperationException("Cannot create connection for a disabled utility type. Please enable the utility type first.");

            // Validate tariff exists, belongs to utility type, and is active
            var tariff = await _context.Tariffs
                .FirstOrDefaultAsync(t => t.Id == dto.TariffId);
            
            if (tariff == null)
                throw new KeyNotFoundException("Tariff not found");
            
            if (tariff.UtilityTypeId != dto.UtilityTypeId)
                throw new InvalidOperationException("Tariff does not belong to the selected utility type. Please select a tariff for the correct utility type.");
            
            if (!tariff.IsActive)
                throw new InvalidOperationException("Cannot assign an inactive tariff to a connection. Please activate the tariff first.");

            var connection = new Connection
            {
                UserId = dto.UserId,
                UtilityTypeId = dto.UtilityTypeId,
                TariffId = dto.TariffId,
                MeterNumber = dto.MeterNumber,
                Status = dto.Status
            };

            _context.Connections.Add(connection);
            await _context.SaveChangesAsync();

            // Reload with includes to get related data
            await _context.Entry(connection).Reference(c => c.User).LoadAsync();
            await _context.Entry(connection).Reference(c => c.UtilityType).LoadAsync();
            await _context.Entry(connection).Reference(c => c.Tariff).LoadAsync();

            await _auditLogService.LogActionAsync("CONNECTION_CREATE", $"Created new connection for user ID {connection.UserId} with meter {connection.MeterNumber}.", currentUserEmail);

            return _mapper.Map<ConnectionDto>(connection);
        }

        public async Task<ConnectionDto> UpdateConnectionAsync(string id, ConnectionDto dto, string currentUserEmail)
        {
            var connection = await _context.Connections.FindAsync(id);
            if (connection == null)
                throw new KeyNotFoundException("Connection not found");

            // Validate status
            if (dto.Status != "Active" && dto.Status != "Inactive")
                throw new ArgumentException("Status must be either 'Active' or 'Inactive'");

            // Check if trying to activate connection or connection is already active
            if (dto.Status == "Active")
            {
                // Check user exists, is not deleted, and is active
                var user = await _context.Users.FindAsync(connection.UserId);
                if (user == null || user.Status == "Deleted")
                    throw new InvalidOperationException("Cannot activate connection for a deleted user. Please transfer the connection to an active user first.");
                
                if (user.Status != "Active")
                    throw new InvalidOperationException("Cannot activate connection for an inactive user. Please activate the user first.");

                // Check utility type is enabled
                var utilityType = await _context.UtilityTypes.FindAsync(connection.UtilityTypeId);
                if (utilityType == null || utilityType.Status != "Enabled")
                    throw new InvalidOperationException("Cannot activate connection for a disabled utility type. Please enable the utility type first.");

                // Check current tariff is active
                var currentTariff = await _context.Tariffs.FindAsync(connection.TariffId);
                if (currentTariff == null || !currentTariff.IsActive)
                {
                    if (dto.TariffId == connection.TariffId)
                        throw new InvalidOperationException("Cannot keep connection active with an inactive tariff. Please activate the tariff first or assign a different active tariff.");
                }
            }

            // Check if meter number is being changed and if it's already taken
            if (dto.MeterNumber != connection.MeterNumber)
            {
                if (await _context.Connections.AnyAsync(c => c.MeterNumber == dto.MeterNumber))
                    throw new InvalidOperationException("Meter number already exists");
            }

            // Check if user is being changed and if it exists and is not deleted
            if (dto.UserId != connection.UserId)
            {
                var newUser = await _context.Users.FindAsync(dto.UserId);
                if (newUser == null || newUser.Status == "Deleted")
                    throw new KeyNotFoundException("User not found or has been deleted");
                
                // Check if new user is active (cannot assign connection to inactive user)
                if (newUser.Status != "Active")
                    throw new InvalidOperationException("Cannot assign connection to an inactive user. Please activate the user first.");
            }

            // Check if utility type is being changed and if it's enabled
            var utilityTypeChanged = dto.UtilityTypeId != connection.UtilityTypeId;
            if (utilityTypeChanged)
            {
                var utilityType = await _context.UtilityTypes.FindAsync(dto.UtilityTypeId);
                if (utilityType == null)
                    throw new KeyNotFoundException("Utility type not found");
                
                if (utilityType.Status != "Enabled")
                    throw new InvalidOperationException("Cannot change connection to a disabled utility type. Please enable the utility type first.");
            }

            var tariffChanged = dto.TariffId != connection.TariffId;
            var targetTariffId = tariffChanged ? dto.TariffId : connection.TariffId;
            
            if (tariffChanged || utilityTypeChanged)
            {
                var tariff = await _context.Tariffs
                    .FirstOrDefaultAsync(t => t.Id == targetTariffId);
                
                if (tariff == null)
                    throw new KeyNotFoundException("Tariff not found");
                
                // Validate tariff belongs to the target utility type
                if (tariff.UtilityTypeId != dto.UtilityTypeId)
                    throw new InvalidOperationException("Tariff does not belong to the selected utility type. Please select a tariff for the correct utility type.");
                
                if (!tariff.IsActive)
                    throw new InvalidOperationException("Cannot assign an inactive tariff to a connection. Please activate the tariff first.");
            }

            connection.UserId = dto.UserId;
            connection.UtilityTypeId = dto.UtilityTypeId;
            connection.TariffId = dto.TariffId;
            connection.MeterNumber = dto.MeterNumber;
            connection.Status = dto.Status;

            await _context.SaveChangesAsync();

            // Reload with includes to get related data
            await _context.Entry(connection).Reference(c => c.User).LoadAsync();
            await _context.Entry(connection).Reference(c => c.UtilityType).LoadAsync();
            await _context.Entry(connection).Reference(c => c.Tariff).LoadAsync();

            await _auditLogService.LogActionAsync("CONNECTION_UPDATE", $"Updated connection for meter {connection.MeterNumber}. New status: {connection.Status}.", currentUserEmail);

            return _mapper.Map<ConnectionDto>(connection);
        }

        public async Task DeleteConnectionAsync(string id, string currentUserEmail)
        {
            var connection = await _context.Connections
                .Include(c => c.Bills)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (connection == null)
                throw new KeyNotFoundException("Connection not found");

            // Check if connection has bills
            if (connection.Bills.Any())
                throw new InvalidOperationException("Cannot delete connection with existing bills. Please handle bills first.");

            // Check if connection has meter readings
            var hasMeterReadings = await _context.MeterReadings.AnyAsync(mr => mr.ConnectionId == id);
            if (hasMeterReadings)
                throw new InvalidOperationException("Cannot delete connection with existing meter readings. Please handle meter readings first.");

            var meterNumber = connection.MeterNumber;
            _context.Connections.Remove(connection);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("CONNECTION_DELETE", $"Deleted connection with meter number '{meterNumber}'.", currentUserEmail);
        }
    }
}