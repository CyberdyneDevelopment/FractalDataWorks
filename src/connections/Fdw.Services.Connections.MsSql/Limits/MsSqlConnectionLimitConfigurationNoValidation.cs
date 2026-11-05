using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.MsSql.Limits;

/// <summary>States that <see cref="MsSqlConnectionLimitConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "MsSqlConnectionLimitConfiguration")]
public sealed class MsSqlConnectionLimitConfigurationNoValidation : UnvalidatedConfiguration<MsSqlConnectionLimitConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlConnectionLimitConfigurationNoValidation"/> class.</summary>
    public MsSqlConnectionLimitConfigurationNoValidation()
        : base("MsSqlConnectionLimitConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
