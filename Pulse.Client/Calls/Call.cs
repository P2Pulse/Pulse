namespace Pulse.Client.Calls;

public record Call(string OtherUser, Task<Stream> Connection);