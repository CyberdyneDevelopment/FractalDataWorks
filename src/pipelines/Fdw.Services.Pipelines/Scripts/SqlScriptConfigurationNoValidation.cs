using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Pipelines.Scripts;

/// <summary>States that <see cref="SqlScriptConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SqlScriptConfiguration")]
public sealed class SqlScriptConfigurationNoValidation : UnvalidatedConfiguration<SqlScriptConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqlScriptConfigurationNoValidation"/> class.</summary>
    public SqlScriptConfigurationNoValidation()
        : base("SqlScriptConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
