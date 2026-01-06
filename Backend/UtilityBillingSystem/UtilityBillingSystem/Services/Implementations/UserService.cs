using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UtilityBillingSystem.Data;
using UtilityBillingSystem.Models.Core;
using UtilityBillingSystem.Models.Dto.User;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditLogService _auditLogService;
        private readonly AppDbContext _context;

        public UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IAuditLogService auditLogService, AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditLogService = auditLogService;
            _context = context;
        }

        public async Task<IEnumerable<UserDto>> GetUsersAsync()
        {
            var userList = await _userManager.Users
                .Where(u => u.Status != "Deleted")
                .ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Name = user.FullName,
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "Consumer",
                    Status = user.Status
                });
            }

            return userDtos.OrderBy(u => u.Name?.ToLowerInvariant() ?? "");
        }

        public async Task<UserDto> GetUserByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || user.Status == "Deleted")
                throw new KeyNotFoundException("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Consumer";

            return new UserDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email ?? "",
                Role = role,
                Status = user.Status
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto, string currentUserEmail)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                throw new InvalidOperationException("Email already exists");

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.Name,
                Status = dto.Status
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            // Assign role
            if (await _roleManager.RoleExistsAsync(dto.Role))
            {
                await _userManager.AddToRoleAsync(user, dto.Role);
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Consumer");
            }

            await _auditLogService.LogActionAsync("USER_CREATE", $"Created new user '{user.FullName}' with role '{dto.Role}'.", currentUserEmail);

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "Consumer",
                Status = user.Status
            };
        }

        public async Task<UserDto> UpdateUserAsync(string id, UpdateUserDto dto, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || user.Status == "Deleted")
                throw new KeyNotFoundException("User not found");

            if (currentUserId == id)
            {
                // Prevent admin from changing their own role
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (dto.Role != currentRoles.FirstOrDefault())
                    throw new InvalidOperationException("Cannot change your own role");
            }

            if (dto.Status == "Deleted")
                throw new InvalidOperationException("Cannot set user status to 'Deleted' via update. Please use the delete endpoint which performs proper validation.");

            // Validate status is valid
            if (dto.Status != "Active" && dto.Status != "Inactive")
                throw new ArgumentException("Status must be either 'Active' or 'Inactive'");

            // Check if status is being changed to "Inactive" and automatically deactivate all active connections
            int deactivatedConnectionsCount = 0;
            if (user.Status != "Inactive" && dto.Status == "Inactive")
            {
                var activeConnections = await _context.Connections
                    .Where(c => c.UserId == id && c.Status == "Active")
                    .ToListAsync();
                
                if (activeConnections.Any())
                {
                    deactivatedConnectionsCount = activeConnections.Count;
                    foreach (var connection in activeConnections)
                    {
                        connection.Status = "Inactive";
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // Check if email is being changed and if it's already taken
            if (dto.Email != user.Email)
            {
                if (await _userManager.FindByEmailAsync(dto.Email) != null)
                    throw new InvalidOperationException("Email already exists");
            }

            user.FullName = dto.Name;
            user.Email = dto.Email;
            user.UserName = dto.Email;
            user.Status = dto.Status;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", updateResult.Errors.Select(e => e.Description)));

            // Update role
            var roles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, roles);

            if (await _roleManager.RoleExistsAsync(dto.Role))
            {
                await _userManager.AddToRoleAsync(user, dto.Role);
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            
            var logMessage = $"Updated user '{user.FullName}' (ID: {user.Id}).";
            if (deactivatedConnectionsCount > 0)
            {
                logMessage += $" Deactivated {deactivatedConnectionsCount} connection(s).";
            }
            
            await _auditLogService.LogActionAsync("USER_UPDATE", logMessage, currentUser?.Email ?? "System");

            var newRoles = await _userManager.GetRolesAsync(user);
            return new UserDto
            {
                Id = user.Id,
                Name = user.FullName,
                Email = user.Email ?? "",
                Role = newRoles.FirstOrDefault() ?? "Consumer",
                Status = user.Status
            };
        }

        public async Task DeleteUserAsync(string id, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || user.Status == "Deleted")
                throw new KeyNotFoundException("User not found");

            // Prevent deleting yourself
            if (currentUserId == id)
                throw new InvalidOperationException("Cannot delete your own account");

            // Check if user has any unpaid bills
            var hasUnpaidBills = await _context.Bills
                .Include(b => b.Connection)
                .AnyAsync(b => b.Connection.UserId == id && b.Status != "Paid");
            
            if (hasUnpaidBills)
                throw new InvalidOperationException("Cannot delete user with unpaid bills. All bills must be paid before deletion.");

            // Check if user has any active connections
            var hasActiveConnections = await _context.Connections
                .AnyAsync(c => c.UserId == id && c.Status == "Active");
            
            if (hasActiveConnections)
                throw new InvalidOperationException("Cannot delete user with active connections. Please deactivate or delete connections first.");

            // Check if user has any pending utility requests
            var hasPendingRequests = await _context.UtilityRequests
                .AnyAsync(ur => ur.UserId == id && ur.Status == "Pending");
            
            if (hasPendingRequests)
                throw new InvalidOperationException("Cannot delete user with pending utility requests. Please resolve or cancel pending requests first.");

            user.Status = "Deleted";
            user.DeletedAt = DateTime.UtcNow;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", updateResult.Errors.Select(e => e.Description)));

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            await _auditLogService.LogActionAsync("USER_DELETE", 
                $"Soft deleted user '{user.FullName}' (Email: {user.Email}, ID: {id}). All bills were paid.", 
                currentUser?.Email ?? "System");
        }
    }
}

