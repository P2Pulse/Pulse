using Microsoft.AspNetCore.Components;
using Pulse.Core.Calls;
using CoreCallPoller = Pulse.Core.Calls.IncomingCallPoller;

namespace Pulse.Client.Calls;

public class IncomingCallPoller
{
    private readonly NavigationManager navigationManager;
    private readonly CoreCallPoller incomingCallPoller;
    private readonly CurrentCallAccessor callAccessor;
    private readonly ICallAcceptor callAcceptor;

    public IncomingCallPoller(NavigationManager navigationManager, CoreCallPoller incomingCallPoller,
        CurrentCallAccessor callAccessor, ICallAcceptor callAcceptor)
    {
        this.navigationManager = navigationManager;
        this.incomingCallPoller = incomingCallPoller;
        this.callAccessor = callAccessor;
        this.callAcceptor = callAcceptor;

        _ = PollForCallsAsync();
    }

    private async Task PollForCallsAsync()
    {
        while (true)
        {
            await Task.Delay(250);
            try
            {
                var username = await incomingCallPoller.PollAsync();
                if (username is null)
                    continue;

                callAccessor.CurrentCall = new Call(username, callAcceptor.AnswerCallAsync());
                
                // TODO: ask user if they want to accept the call
                navigationManager.NavigateTo("/ActiveCall");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}