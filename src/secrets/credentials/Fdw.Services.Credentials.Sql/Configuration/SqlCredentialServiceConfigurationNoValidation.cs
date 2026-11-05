using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Credentials.Sql.Configuration;

/// <summary>States that <see cref="SqlCredentialServiceConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SqlCredentialServiceConfiguration")]
public sealed class SqlCredentialServiceConfigurationNoValidation : UnvalidatedConfiguration<SqlCredentialServiceConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqlCredentialServiceConfigurationNoValidation"/> class.</summary>
    public SqlCredentialServiceConfigurationNoValidation()
        : base("SqlCredentialServiceConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
