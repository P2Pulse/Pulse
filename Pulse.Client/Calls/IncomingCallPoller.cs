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

    public event Func<string, Task<bool>> OnIncomingCall; // I'm sorry :(
    public event Action? OnCallAnswer;

    private async Task PollForCallsAsync()
    {
        while (true)
        {
            await Task.Delay(250);
            
            if (accessTokenStorage.AccessToken is null)
                continue;
            
            if (callAccessor.CurrentCall is not null)
                continue;

            var incomingCallPoller = serviceProvider.GetRequiredService<CoreCallPoller>();

            try
            {
                var username = await incomingCallPoller.PollAsync();
                if (username is null)
                    continue;
                
                var shouldAnswerCall = await OnIncomingCall.Invoke(username);

                if (!shouldAnswerCall)
                {
                    await callAcceptor.DeclineCallAsync();
                    continue;
                }

                callAccessor.CurrentCall = new Call(username, callAcceptor.AnswerCallAsync());

                OnCallAnswer?.Invoke();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}