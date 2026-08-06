using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Operations.Components.Dashboard;

/// <summary>
/// Immutable context for the operations dashboard widget.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class OperationsDashboardContext : ProviderContextBase
{
    /// <summary>Gets the recent activity entries.</summary>
    public IReadOnlyList<ActivityEntryPayload> Activities { get; init; } = [];



}
