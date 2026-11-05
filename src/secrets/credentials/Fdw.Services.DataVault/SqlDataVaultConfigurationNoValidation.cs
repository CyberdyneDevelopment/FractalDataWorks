using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.DataVault;

/// <summary>States that <see cref="SqlDataVaultConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SqlDataVaultConfiguration")]
public sealed class SqlDataVaultConfigurationNoValidation : UnvalidatedConfiguration<SqlDataVaultConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqlDataVaultConfigurationNoValidation"/> class.</summary>
    public SqlDataVaultConfigurationNoValidation()
        : base("SqlDataVaultConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
