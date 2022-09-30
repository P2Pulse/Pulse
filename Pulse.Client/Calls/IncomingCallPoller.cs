using Pulse.Core.Authentication;
using Pulse.Core.Calls;
using CoreCallPoller = Pulse.Core.Calls.IncomingCallPoller;

namespace Pulse.Client.Calls;

public class IncomingCallPoller
{
    private readonly IServiceProvider serviceProvider;
    private readonly CurrentCallAccessor callAccessor;
    private readonly ICallAcceptor callAcceptor;
    private readonly IAccessTokenStorage accessTokenStorage;

    public IncomingCallPoller(IServiceProvider serviceProvider, CurrentCallAccessor callAccessor, 
        ICallAcceptor callAcceptor, IAccessTokenStorage accessTokenStorage)
    {
        this.serviceProvider = serviceProvider;
        this.callAccessor = callAccessor;
        this.callAcceptor = callAcceptor;
        this.accessTokenStorage = accessTokenStorage;

        _ = PollForCallsAsync();
    }

    public event Action? OnIncomingCall;

    private async Task PollForCallsAsync()
    {
        while (true)
        {
            await Task.Delay(250);
            
            if (accessTokenStorage.AccessToken is null)
                continue;
            
            if (callAccessor.CurrentCall is not null) // TODO: make this null at the end of the call
                continue;

            var incomingCallPoller = serviceProvider.GetRequiredService<CoreCallPoller>();

            try
            {
                var username = await incomingCallPoller.PollAsync();
                if (username is null)
                    continue;

                callAccessor.CurrentCall = new Call(username, callAcceptor.AnswerCallAsync());

                // TODO: ask user if they want to accept the call
                OnIncomingCall?.Invoke();

                break; // TODO: allow more than one call in a session
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}