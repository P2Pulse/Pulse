using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Pulse.Server.Contracts;

namespace Pulse.Server.Core;

public class InMemoryCallMatcher
{
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };
    private readonly IMemoryCache cache;

    public InMemoryCallMatcher(IMemoryCache cache)
    {
        this.cache = cache;
    }

    public Task InitiateCallAsync(string callId, InitiateCallRequest request, string callerUsername)
    {
        // TODO: If the caller is already in a call/there is a call waiting for him, return an error/handle it nicely.
        // TODO: If A is calling B and B didn't answer yet, C can call B and fuck A's call.
        // TODO: Add a timeout to calls on the server side.
        var myTaskCompletionSource = new TaskCompletionSource<ConnectionDetails>();
        cache.Set(PendingConnectionKey(callerUsername), new PendingConnection(callId, IsIncoming: false,
            request.CalleeUserName, myTaskCompletionSource), CacheOptions);
        cache.Set(PendingConnectionKey(request.CalleeUserName), new PendingConnection(callId, IsIncoming: true, 
            OtherUsername: callerUsername, ConnectionDetails: new TaskCompletionSource<ConnectionDetails>()), CacheOptions);

        return myTaskCompletionSource.Task;
    }
    
    public IncomingCall? PollForIncomingCall(string username)
    {
        return TryGetPendingConnection(username, out var pendingConnection) && pendingConnection.IsIncoming
            ? new IncomingCall(pendingConnection.OtherUsername, pendingConnection.CallId)
            : null;
    }

    public Task<ConnectionDetails> JoinCallAsync(JoinCallRequest request, string userName)
    {
        if (!TryGetPendingConnection(userName, out var myPendingConnection))
            throw new Exception("No pending call");

        if (!TryGetPendingConnection(myPendingConnection.OtherUsername, out var otherPersonConnection))
        {
            cache.Remove(PendingConnectionKey(userName));
            throw new Exception("Something went wrong in the initialization process. Make sure to only join a call " +
                                "once you received a successful initiation response or a polling response.");
        }

        var callId = string.Join(',', new[] { userName, myPendingConnection.OtherUsername }.OrderBy(x => x));
        var iv = cache.GetOrCreate($"initialization-vectors:{callId}", cacheEntry =>
        {
            cacheEntry.SetOptions(CacheOptions);
            return Aes.Create().IV;
        });
        
        var otherPersonConnectionDetails = new ConnectionDetails(request.IPAddress, request.MinPort, request.MaxPort, 
            request.PublicKey, iv);
        
        otherPersonConnection.ConnectionDetails.SetResult(otherPersonConnectionDetails);

        return myPendingConnection.ConnectionDetails.Task.ContinueWith(t =>
        {
            cache.Remove(PendingConnectionKey(userName));
            return t.Result;
        });
    }
    
    public void DeclineIncomingCall(string username)
    {
        if (!TryGetPendingConnection(username, out var pendingConnection)) 
            return;
        
        cache.Remove(PendingConnectionKey(username));

        if (!TryGetPendingConnection(pendingConnection.OtherUsername, out var otherUserConnection)) 
            return;
        
        otherUserConnection.ConnectionDetails.TrySetCanceled();
        
        cache.Remove(PendingConnectionKey(pendingConnection.OtherUsername));
    }
    
    private static string PendingConnectionKey(string username) => $"pending-connections:{username}";

    private bool TryGetPendingConnection(string username, [NotNullWhen(true)]out PendingConnection? pendingConnection)
    {
        return cache.TryGetValue<PendingConnection>(PendingConnectionKey(username), out pendingConnection);
    }

    private record PendingConnection(string CallId, bool IsIncoming, string OtherUsername,
        TaskCompletionSource<ConnectionDetails> ConnectionDetails);
}