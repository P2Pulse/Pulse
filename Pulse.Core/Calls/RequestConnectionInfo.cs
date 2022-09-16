namespace Pulse.Core.Calls;

internal record RequestConnectionInfo(
    string IPAddress,
    int MinPort,
    int MaxPort,
    byte[] PublicKey
);