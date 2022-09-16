namespace Pulse.Core.Calls;

internal record ConnectionInfo(
    string remoteIPAddress,
    int MinPort,
    int MaxPort,
    byte[] PublicKey,
    byte[] IV
);