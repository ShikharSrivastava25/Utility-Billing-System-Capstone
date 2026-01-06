using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityBillingSystem.Models.Dto.User;
using UtilityBillingSystem.Services.Interfaces;

namespace UtilityBillingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userService.GetUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(user);
        }

        [HttpGet("profile")]
        [Authorize(Roles = "Consumer,Admin")]
        public async Task<ActionResult<UserDto>> GetMyProfile()
        {
            var user = await _userService.GetUserByIdAsync(CurrentUserId);
            return Ok(user);
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> UpdateMyProfile([FromBody] UpdateUserDto dto)
        {
            var existingUser = await _userService.GetUserByIdAsync(CurrentUserId);
            dto.Role = existingUser.Role;

            var user = await _userService.UpdateUserAsync(CurrentUserId, dto, CurrentUserId);
            return Ok(user);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto)
        {
            var user = await _userService.CreateUserAsync(dto, CurrentUserEmail);
            return Ok(user);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserDto>> UpdateUser(string id, [FromBody] UpdateUserDto dto)
        {
            var user = await _userService.UpdateUserAsync(id, dto, CurrentUserId);
            return Ok(user);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _userService.DeleteUserAsync(id, CurrentUserId);
            return Ok(new { message = "User deleted successfully" });
        }
    }
}


