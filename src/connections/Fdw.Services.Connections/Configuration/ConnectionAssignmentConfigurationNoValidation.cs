using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.Configuration;

/// <summary>States that <see cref="ConnectionAssignmentImplementationConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "ConnectionAssignmentImplementationConfiguration")]
public sealed class ConnectionAssignmentConfigurationNoValidation : UnvalidatedConfiguration<ConnectionAssignmentImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionAssignmentConfigurationNoValidation"/> class.</summary>
    public ConnectionAssignmentConfigurationNoValidation()
        : base("ConnectionAssignmentImplementationConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
