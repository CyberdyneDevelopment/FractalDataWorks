using System;

namespace Fdw.Services.Data.SignalR;

/// <summary>
/// Event raised when schema discovery starts.
/// </summary>
public sealed record SchemaDiscoveryStartedEvent(
    string DiscoveryId,
    string ConnectionName,
    string? SchemaFilter,
    DateTimeOffset StartedAt,
    string RequestedBy);