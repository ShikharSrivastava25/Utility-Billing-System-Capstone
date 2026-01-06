using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UtilityBillingSystem.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected string CurrentUserId => 
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        
        protected string CurrentUserEmail => 
            User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
    }
}

