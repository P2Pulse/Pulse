using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Pulse.Server.Core;

public class InMemoryCallMatcher
{
    private readonly ConcurrentDictionary<string, ConnectionDetails> pendingCalls = new();
    private readonly ConcurrentDictionary<string, ConnectionDetails> acceptedCalls = new();

    public async Task<ConnectionDetails> InitiateCallAsync(InitiateCallRequest request, IPAddress callerIp)
    {
        pendingCalls[request.CalleeUserName] =
            new ConnectionDetails(callerIp.ToString(), request.MinPort, request.MaxPort);

        while (true)
        {
            await Task.Delay(20);
            if (acceptedCalls.TryRemove(request.CalleeUserName, out var connectionDetails))
                return connectionDetails;
        }
    }

    public bool PollForIncomingCall(string userName)
    {
        return pendingCalls.ContainsKey(userName);
    }

    public ConnectionDetails AcceptIncomingCall(AcceptCallRequest request, string userName, string ipAddress)
    {
        if (!pendingCalls.TryRemove(userName, out var connectionDetails))
            throw new Exception("No pending call");

        acceptedCalls[userName] = new ConnectionDetails(ipAddress, request.MinPort, request.MaxPort);
        
        return connectionDetails;
    }
}