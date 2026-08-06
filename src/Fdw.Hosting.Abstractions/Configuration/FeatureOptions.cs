using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Feature flags for enabling/disabling optional FDW modules.
/// Binds to the "FdwHost:Features" section in appsettings.json.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class FeatureOptions
{
    /// <summary>
    /// Gets or sets whether the Data module (connections, DataGateway) is enabled.
    /// Default is true.
    /// </summary>
    public bool Data { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the Multitenancy module is enabled.
    /// Default is false.
    /// </summary>
    public bool Multitenancy { get; set; }

    /// <summary>
    /// Gets or sets whether the Authorization module (roles, permissions) is enabled.
    /// Default is false.
    /// </summary>
    public bool Authorization { get; set; }

    /// <summary>
    /// Gets or sets whether the Caching module is enabled.
    /// Default is false.
    /// </summary>
    public bool Caching { get; set; }

    /// <summary>
    /// Gets or sets whether the Messaging module is enabled.
    /// Default is false.
    /// </summary>
    public bool Messaging { get; set; }

    /// <summary>
    /// Gets or sets whether health checks are enabled.
    /// Default is true.
    /// </summary>
    public bool HealthChecks { get; set; } = true;
}
