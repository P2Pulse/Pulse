using Pulse.Core.Calls;
using CoreCallPoller = Pulse.Core.Calls.IncomingCallPoller;

namespace Pulse.Client.Calls;

public class IncomingCallPoller
{
    private readonly CoreCallPoller incomingCallPoller;
    private readonly CurrentCallAccessor callAccessor;
    private readonly ICallAcceptor callAcceptor;

    public IncomingCallPoller(CoreCallPoller incomingCallPoller, CurrentCallAccessor callAccessor, 
        ICallAcceptor callAcceptor)
    {
        this.incomingCallPoller = incomingCallPoller;
        this.callAccessor = callAccessor;
        this.callAcceptor = callAcceptor;

        _ = PollForCallsAsync();
    }

    public event Action? OnIncomingCall;

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
                OnIncomingCall?.Invoke();

                break; // TODO: allow more than one call in a session
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}