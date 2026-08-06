using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// A single deterministic-input declaration: one parameter a command accepts, the values it
/// permits, and whether it must be present.
/// </summary>
/// <remarks>
/// Why not <c>[ManagedConfiguration]</c>: for Phase 0/1 this is carried inline as a nested list on
/// <c>PreApprovedCommandConfiguration.ParameterAllowList</c>, bound directly from
/// <c>aegisSchema.json</c> via <c>IOptions</c> — no independent ConfigurationDb table exists yet.
/// A dedicated child table (mirroring <c>DataContainerFieldConfiguration</c>) is Phase 2 work, once
/// <c>request_action</c> persists to ConfigurationDb.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ParameterAllowEntry
{
    /// <summary>Gets or sets the parameter name this entry declares.</summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>Gets or sets the values permitted for this parameter.</summary>
    public IList<string> PermittedValues { get; set; } = new List<string>();

    /// <summary>Gets or sets whether this parameter must be present on every request.</summary>
    public bool Required { get; set; }
}
