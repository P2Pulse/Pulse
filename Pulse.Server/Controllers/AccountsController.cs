using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Pulse.Server.Controllers;

/// <summary>
/// Manages user accounts
/// </summary>
[ApiController]
[Route("[controller]")]
public class AccountsController : ControllerBase
{
    private readonly UserManager<IdentityUser> userManager;

    public AccountsController(UserManager<IdentityUser> userManager)
    {
        this.userManager = userManager;
    }

    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="request">The registration request</param>
    [HttpPost]
    public async Task<IActionResult> RegisterAsync(RegistrationRequest request)
    {
        var user = new IdentityUser { UserName = request.UserName };
        var result = await userManager.CreateAsync(user, request.Password);
        
        if (result.Succeeded)
            return Ok();
        
        return BadRequest(result.Errors);
    }
    
    public record RegistrationRequest(string UserName, string Password);
}