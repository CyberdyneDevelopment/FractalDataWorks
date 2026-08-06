namespace Fdw.Services.Data.SignalR;

/// <summary>
/// Event raised when schema discovery fails.
/// </summary>
public sealed record SchemaDiscoveryFailedEvent(
    string DiscoveryId,
    string ErrorCode,
    string ErrorMessage,
    int PartialObjectsDiscovered,
    bool IsRetryable);