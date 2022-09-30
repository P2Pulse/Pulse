using CoreCall = Pulse.Core.Calls.Call;

namespace Pulse.Client.Calls;

public record Call(string OtherUser, Task<CoreCall> Connection);