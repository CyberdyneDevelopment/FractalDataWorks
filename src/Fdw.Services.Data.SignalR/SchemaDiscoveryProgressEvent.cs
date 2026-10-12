namespace Fdw.Services.Data.SignalR;

/// <summary>
/// Event raised to report schema discovery progress.
/// </summary>
public sealed record SchemaDiscoveryProgressEvent(
    string DiscoveryId,
    int PercentComplete,
    string CurrentStep,
    int ObjectsDiscovered,
    int? EstimatedTotal);