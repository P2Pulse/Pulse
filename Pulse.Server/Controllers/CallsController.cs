﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<ConnectionDetails>> InitiateNewCallAsync([FromBody] InitiateCallRequest request)
    {
        return await callMatcher.InitiateCallAsync(request, HttpContext.Connection.RemoteIpAddress!, User.Identities.First().Name!);
    }
    
    /// <summary>
    /// Polls for incoming calls
    /// </summary>
    /// <returns>Username of caller if there is an awaiting incoming call, null otherwise</returns>
    [HttpGet]
    public ActionResult<CallRequest> PollForIncomingCallAsync()
    {
        var userName = User.Identities.First().Name!;
        return callMatcher.PollForIncomingCall(userName);
    }
    
    /// <summary>
    /// Accepts an incoming call
    /// </summary>
    /// <param name="request">Details about how to connect the call</param>
    /// <returns>Connection details</returns>
    [HttpPost("accept")]
    public ActionResult<ConnectionDetails> AcceptIncomingCallAsync([FromBody] AcceptCallRequest request)
    {
        var userName = User.Identities.First().Name;
        return callMatcher.AcceptIncomingCall(request, userName, HttpContext.Connection.RemoteIpAddress!.ToString());
    }
}