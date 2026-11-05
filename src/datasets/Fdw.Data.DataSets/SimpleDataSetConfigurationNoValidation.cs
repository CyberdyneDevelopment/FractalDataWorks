using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="SimpleDataSetConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SimpleDataSetConfiguration")]
public sealed class SimpleDataSetConfigurationNoValidation : UnvalidatedConfiguration<SimpleDataSetConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SimpleDataSetConfigurationNoValidation"/> class.</summary>
    public SimpleDataSetConfigurationNoValidation()
        : base("SimpleDataSetConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
