namespace Pulse.Core.Calls;

internal record ConnectionInfo(
    string IPAddress,
    int MinPort,
    int MaxPort,
    byte[] PublicKey,
    byte[]? IV
);
