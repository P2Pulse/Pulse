using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Pulse.Server.Controllers;

/// <summary>
/// Manages user sessions (logins)
/// </summary>
[ApiController]
[Route("[controller]")]
public class SessionsController : ControllerBase
{
    private readonly SignInManager<IdentityUser> signInManager;
    private readonly IConfiguration configuration;

    public SessionsController(SignInManager<IdentityUser> signInManager, IConfiguration configuration)
    {
        this.signInManager = signInManager;
        this.configuration = configuration;
    }
    
    /// <summary>
    /// Logs a user in.
    /// </summary>
    /// <response code="200">Returns the access token</response>
    /// <response code="400">If the credentials are invalid</response>
    [HttpPost]
    public async Task<ActionResult<AuthenticationResult>> LogInAsync([FromBody] AuthenticationCredentials credentials)
    {
        var result = await signInManager.PasswordSignInAsync(credentials.UserName, credentials.Password, false, false);
        if (!result.Succeeded)
        {
            return BadRequest();
        }
        var user = await signInManager.UserManager.FindByNameAsync(credentials.UserName);
        var token = GenerateAccessToken(user);
        return new AuthenticationResult(token);
    }

    private string GenerateAccessToken(IdentityUser user)
    {
        var jwtHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["Authentication:SecretKey"]);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName)
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = jwtHandler.CreateToken(tokenDescriptor);
        
        return jwtHandler.WriteToken(token);
    }

    public record AuthenticationCredentials(string UserName, string Password);
    public record AuthenticationResult(string AccessToken);
}