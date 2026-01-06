using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.UtilityRequest;
using UtilityBillingSystem.Models.Dto.Connection;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Services
{
    public class UtilityRequestService : IUtilityRequestService
    {
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IConnectionService _connectionService;
        private readonly IMapper _mapper;

        public UtilityRequestService(AppDbContext context, IAuditLogService auditLogService, IConnectionService connectionService, IMapper mapper)
        {
            _context = context;
            _auditLogService = auditLogService;
            _connectionService = connectionService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UtilityRequestDto>> GetRequestsAsync()
        {
            var requests = await _context.UtilityRequests.ToListAsync();
            return _mapper.Map<IEnumerable<UtilityRequestDto>>(requests);
        }

        public async Task<IEnumerable<UtilityRequestDto>> GetRequestsForUserAsync(string userId)
        {
            var requests = await _context.UtilityRequests
                .Where(ur => ur.UserId == userId)
                .ToListAsync();
            return _mapper.Map<IEnumerable<UtilityRequestDto>>(requests);
        }

        public async Task<UtilityRequestDto> GetRequestByIdAsync(string id)
        {
            var request = await _context.UtilityRequests.FindAsync(id);
            if (request == null)
                throw new KeyNotFoundException("Request not found");

            return _mapper.Map<UtilityRequestDto>(request);
        }

        public async Task<UtilityRequestDto> CreateRequestAsync(UtilityRequestDto dto, string userId, bool isAdmin)
        {
            var request = new UtilityRequest
            {
                UserId = isAdmin ? dto.UserId : userId,
                UtilityTypeId = dto.UtilityTypeId,
                Status = "Pending",
                RequestDate = DateTime.UtcNow
            };

            _context.UtilityRequests.Add(request);
            await _context.SaveChangesAsync();

            return _mapper.Map<UtilityRequestDto>(request);
        }

        public async Task<ConnectionDto> ApproveRequestAsync(string id, ApproveRequestDto dto, string currentUserEmail)
        {
            var request = await _context.UtilityRequests
                .Include(ur => ur.User)
                .Include(ur => ur.UtilityType)
                .FirstOrDefaultAsync(ur => ur.Id == id);

            if (request == null)
                throw new KeyNotFoundException("Request not found");

            if (request.Status != "Pending")
                throw new InvalidOperationException("Request has already been processed");

            // Check if utility type is enabled
            if (request.UtilityType == null)
                throw new KeyNotFoundException("Utility type not found");
            
            if (request.UtilityType.Status != "Enabled")
                throw new InvalidOperationException("Cannot approve request for a disabled utility type. Please enable the utility type first.");

            var connectionDto = new ConnectionDto
            {
                UserId = request.UserId,
                UtilityTypeId = request.UtilityTypeId,
                TariffId = dto.TariffId,
                MeterNumber = dto.MeterNumber,
                Status = "Active"
            };

            var connection = await _connectionService.CreateConnectionAsync(connectionDto, currentUserEmail);

            // Update request status
            request.Status = "Approved";
            request.DecisionDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("REQUEST_APPROVE", $"Approved request ID {request.Id} for user ID {request.UserId}.", currentUserEmail);

            return connection;
        }

        public async Task<UtilityRequestDto> RejectRequestAsync(string id, string currentUserEmail)
        {
            var request = await _context.UtilityRequests.FindAsync(id);
            if (request == null)
                throw new KeyNotFoundException("Request not found");

            if (request.Status != "Pending")
                throw new InvalidOperationException("Request has already been processed");

            request.Status = "Rejected";
            request.DecisionDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync("REQUEST_REJECT", $"Rejected request ID {request.Id}.", currentUserEmail);

            return _mapper.Map<UtilityRequestDto>(request);
        }
    }
}

