﻿using System.Collections.Immutable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pulse.Server.Contracts;
using Pulse.Server.Core;
using Pulse.Server.Persistence;

namespace Pulse.Server.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CallsController : ControllerBase
{
    private readonly InMemoryCallMatcher callMatcher;
    private readonly MongoCallRepository callRepository;

    public CallsController(InMemoryCallMatcher callMatcher, MongoCallRepository callRepository)
    {
        this.callMatcher = callMatcher;
        this.callRepository = callRepository;
    }
    
    /// <summary>
    /// Initiates a new call
    /// </summary>
    /// <param name="request">Details about the call</param>
    /// <returns>Connection details</returns>
    [HttpPost]
    public async Task<ActionResult<Call>> InitiateNewCallAsync([FromBody] InitiateCallRequest request)
    {
        var call = new Call
        {
            Id = Guid.NewGuid().ToString(),
            CallTime = DateTime.UtcNow,
            Caller = GetCurrentUsername(),
            Callee = request.CalleeUserName
        };

        try
        {
            await callMatcher.InitiateCallAsync(Guid.NewGuid().ToString(), request, GetCurrentUsername());
            call.AnswerTime = DateTime.UtcNow;
        }
        catch (Exception e) when (e is OperationCanceledException or TimeoutException)
        {
            call.EndTime = DateTime.UtcNow;
            return StatusCode(StatusCodes.Status418ImATeapot);
        }
        finally
        {
            await callRepository.SaveAsync(call);
        }

        return Ok(call);
    }

    /// <summary>
    /// Polls for incoming calls
    /// </summary>
    /// <returns>Username of caller if there is an awaiting incoming call, null otherwise</returns>
    [HttpGet("incoming")]
    public ActionResult<IncomingCall> PollForIncomingCallAsync()
    {
        var incomingCall = callMatcher.PollForIncomingCall(GetCurrentUsername());
        
        if (incomingCall is null)
            return NotFound(new {Message = "No pending incoming call found for the current user"});

        return incomingCall;
    }

    [HttpDelete("incoming")]
    public IActionResult DeclineIncomingCall()
    {
        callMatcher.DeclineIncomingCall(GetCurrentUsername());
        
        return NoContent();
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

    [HttpPut("{callId}/ending")]
    public async Task<IActionResult> MarkCallAsEndedAsync(string callId)
    {
        var call = await callRepository.GetByIdAsync(callId);
        call.EndTime = DateTime.UtcNow;
        await callRepository.SaveAsync(call);

        return NoContent();
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<Call>>> GetRecentCallsAsync()
    {
        var calls = await callRepository.GetRecentCallsAsync(GetCurrentUsername());
        return Ok(calls);
    }

    [HttpGet("frequent-contacts")]
    public async Task<ActionResult<IEnumerable<string>>> GetFrequentContactsAsync()
    {
        var calls = await callRepository.GetRecentCallsAsync(GetCurrentUsername(), limit: 500);
        var frequentContacts = calls
            .Select(c => new
            {
                OtherUser = c.Caller == GetCurrentUsername() ? c.Callee : c.Caller,
                Score = 1 / Math.Max(1, (DateTime.UtcNow - c.CallTime).TotalDays)
            })
            .GroupBy(c => c.OtherUser)
            .Select(u => new
            {
                User = u.Key,
                Score = u.Select(c => c.Score).Sum()
            })
            .OrderByDescending(u => u.Score)
            .Select(u => u.User)
            .ToImmutableList();

        return Ok(frequentContacts);
    }

    private string GetCurrentUsername()
    {
        return User.Identity!.Name!;
    }
}