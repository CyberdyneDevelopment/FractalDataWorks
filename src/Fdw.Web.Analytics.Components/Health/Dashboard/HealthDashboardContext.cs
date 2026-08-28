using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.UI.Providers;

namespace Fdw.Web.Analytics.Components.Health.Dashboard;

/// <summary>
/// Immutable context for the Health Dashboard headless provider.
/// Aggregates system health and per-service throughput data.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HealthDashboardContext : ProviderContextBase
{
    /// <summary>Gets the system health snapshot.</summary>
    public SystemHealthSnapshot? SystemHealth { get; init; }

    /// <summary>Gets the per-service throughput data.</summary>
    public IReadOnlyDictionary<string, ThroughputData> ServiceThroughput { get; init; } =
        new Dictionary<string, ThroughputData>(StringComparer.OrdinalIgnoreCase);



}
