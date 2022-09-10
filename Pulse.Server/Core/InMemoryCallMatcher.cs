using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Pulse.Server.Core;

public class InMemoryCallMatcher
{
    private readonly ConcurrentDictionary<string, ConnectionDetails> pendingCalls = new();
    private readonly ConcurrentDictionary<string, ConnectionDetails> acceptedCalls = new();

    public async Task<ConnectionDetails> InitiateCallAsync(InitiateCallRequest request, string callerUsername)
    {
        // FIXME: If the caller is already in a call/there is a call waiting for him, return an error/handle it nicely.
        // FIXME: If A is calling B and B didn't answer yet, C can call B and fuck A's call.
        // TODO: Add a timeout to calls on the server side.

        pendingCalls[request.CalleeUserName] =
            new ConnectionDetails(request.callerIPv4Address, request.MinPort, request.MaxPort, callerUsername);

        while (true)
        {
            await Task.Delay(50);
            if (acceptedCalls.TryRemove(request.CalleeUserName, out var connectionDetails))
                return connectionDetails;
        }
    }

    public CallRequest PollForIncomingCall(string userName)
    {
        if (pendingCalls.ContainsKey(userName))
        {
            var connectionDetails = pendingCalls[userName];
            return new CallRequest(calling: true, connectionDetails.CallerUsername);
        }

        return new CallRequest(calling: false);
    }

    public ConnectionDetails AcceptIncomingCall(AcceptCallRequest request, string userName)
    {
        if (!pendingCalls.TryRemove(userName, out var connectionDetails))
            throw new Exception("No pending call");

        acceptedCalls[userName] = new ConnectionDetails(request.calleeIPv4Address, request.MinPort, request.MaxPort, userName);

        return connectionDetails;
    }
}