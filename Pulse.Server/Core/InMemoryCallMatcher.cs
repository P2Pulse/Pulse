using System.Collections.Concurrent;
using System.Security.Cryptography;
using Pulse.Server.Contracts;

namespace Pulse.Server.Core;

public class InMemoryCallMatcher
{
    private readonly ConcurrentDictionary<string, PendingConnection> pendingConnections = new();
    private readonly ConcurrentDictionary<string, byte[]> initializationVectors = new(); // TODO: IVs are never deleted so this only grows over time

    public Task InitiateCallAsync(InitiateCallRequest request, string callerUsername)
    {
        // TODO: If the caller is already in a call/there is a call waiting for him, return an error/handle it nicely.
        // TODO: If A is calling B and B didn't answer yet, C can call B and fuck A's call.
        // TODO: Add a timeout to calls on the server side.
        var myTaskCompletionSource = new TaskCompletionSource<ConnectionDetails>();
        pendingConnections[callerUsername] = new PendingConnection(IsIncoming: false, request.CalleeUserName, 
            myTaskCompletionSource);
        pendingConnections[request.CalleeUserName] = new PendingConnection(
            IsIncoming: true, callerUsername, new TaskCompletionSource<ConnectionDetails>());

        return myTaskCompletionSource.Task;
    }

    public IncomingCall? PollForIncomingCall(string userName)
    {
        return pendingConnections.TryGetValue(userName, out var pendingConnection) && pendingConnection.IsIncoming
            ? new IncomingCall(pendingConnection.OtherUsername)
            : null;
    }

    public Task<ConnectionDetails> JoinCallAsync(JoinCallRequest request, string userName)
    {
        if (!pendingConnections.TryGetValue(userName, out var myPendingConnection))
            throw new Exception("No pending call");

        if (!pendingConnections.TryGetValue(myPendingConnection.OtherUsername, out var otherPersonConnection))
        {
            pendingConnections.Remove(userName, out _);
            throw new Exception("Something went wrong in the initialization process. Make sure to only join a call " +
                                "once you received a successful initiation response or a polling response.");
        }

        var callId = string.Join(',', new[] { userName, myPendingConnection.OtherUsername }.OrderBy(x => x));
        var iv = initializationVectors.GetOrAdd(callId, _ => Aes.Create().IV);
        
        var otherPersonConnectionDetails = new ConnectionDetails(request.IPAddress, request.MinPort, request.MaxPort, 
            request.PublicKey, iv);
        
        otherPersonConnection.ConnectionDetails.SetResult(otherPersonConnectionDetails);

        return myPendingConnection.ConnectionDetails.Task.ContinueWith(t =>
        {
            pendingConnections.Remove(userName, out _);
            return t.Result;
        });
    }

    private record PendingConnection(bool IsIncoming, string OtherUsername, TaskCompletionSource<ConnectionDetails> ConnectionDetails);
}