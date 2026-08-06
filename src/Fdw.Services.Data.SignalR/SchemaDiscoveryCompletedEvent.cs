using System;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.SignalR;

/// <summary>
/// Event raised when schema discovery completes successfully.
/// </summary>
public sealed record SchemaDiscoveryCompletedEvent(
    string DiscoveryId,
    SchemaDiscoverySummary Summary,
    TimeSpan Duration);