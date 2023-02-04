namespace Pulse.Core.Calls;

public record Call(string? CallId, Stream Stream, string EncryptedStreamCredentialsHash);