using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pulse.Server.Contracts;
using Pulse.Server.Core;

namespace Pulse.Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CallsController : ControllerBase
{
    private readonly InMemoryCallMatcher callMatcher;
    private readonly UserManager<IdentityUser> userManager;

    public CallsController(InMemoryCallMatcher callMatcher, UserManager<IdentityUser> userManager)
    {
        this.callMatcher = callMatcher;
        this.userManager = userManager;
    }
    
    /// <summary>
    /// Initiates a new call
    /// </summary>
    /// <param name="request">Details about the call</param>
    /// <returns>Connection details</returns>
    [HttpPost]
    public IActionResult InitiateNewCall([FromBody] InitiateCallRequest request)
    {
        callMatcher.InitiateCall(request, GetCurrentUsername());
        return NoContent();
    }

    /// <summary>
    /// Polls for incoming calls
    /// </summary>
    /// <returns>Username of caller if there is an awaiting incoming call, null otherwise</returns>
    [HttpGet("/incoming")]
    public ActionResult<IncomingCall> PollForIncomingCallAsync()
    {
        var incomingCall = callMatcher.PollForIncomingCall(GetCurrentUsername());
        
        if (incomingCall is null)
            return NotFound(new {Message = "No pending incoming call found for the current user"});

        return incomingCall;
    }
    
    /// <summary>
    /// Join a pending call
    /// </summary>
    /// <param name="request">Details about how to connect the call</param>
    /// <returns>Connection details</returns>
    [HttpPost("join")]
    public async Task<ActionResult<ConnectionDetails>> JoinPendingCallAsync([FromBody] JoinCallRequest request)
    {
        return await callMatcher.JoinCallAsync(request, GetCurrentUsername());
    }
    
    private string GetCurrentUsername()
    {
        return User.Identity!.Name!;
    }
}