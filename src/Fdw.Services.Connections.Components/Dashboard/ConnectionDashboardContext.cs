using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Connections.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Connections.Components.Dashboard;

/// <summary>
/// Immutable context for the connection dashboard widget.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ConnectionDashboardContext : ProviderContextBase
{
    /// <summary>Gets the total number of connections.</summary>
    public int TotalConnections { get; init; }

    /// <summary>Gets the number of healthy connections.</summary>
    public int HealthyConnections { get; init; }

    /// <summary>Gets the connection summaries.</summary>
    public IReadOnlyList<ConnectionPayload> Connections { get; init; } = [];



}
