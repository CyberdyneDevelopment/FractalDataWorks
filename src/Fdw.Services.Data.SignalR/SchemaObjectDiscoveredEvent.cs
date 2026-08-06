namespace Fdw.Services.Data.SignalR;

/// <summary>
/// Event raised when a schema object (table/view) is discovered.
/// </summary>
public sealed record SchemaObjectDiscoveredEvent(
    string DiscoveryId,
    string SchemaName,
    string ObjectName,
    string ObjectType,
    int ColumnCount);