using Fdw.Collections.Attributes;

namespace Fdw.Services.Data;

/// <summary>
/// The FDW-level setting (FederationSettings): the host's configuration, else its override, else what the strategy or the Federated kind
/// declares, resolved at registration.
/// </summary>
/// <remarks>Why not "Fdw": an option's generated member takes its name, and a member named Fdw hides the root namespace Fdw in generated code.</remarks>
[TypeOption(typeof(FederationValueSources), "Configuration")]
public sealed class ConfigurationFederationValueSource : FederationValueSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurationFederationValueSource"/> class.</summary>
    public ConfigurationFederationValueSource()
        : base("Configuration")
    {
    }
}
