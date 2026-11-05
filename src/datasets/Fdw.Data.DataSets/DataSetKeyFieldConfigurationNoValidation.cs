using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets;

/// <summary>States that <see cref="DataSetKeyFieldConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataSetKeyFieldConfiguration")]
public sealed class DataSetKeyFieldConfigurationNoValidation : UnvalidatedConfiguration<DataSetKeyFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetKeyFieldConfigurationNoValidation"/> class.</summary>
    public DataSetKeyFieldConfigurationNoValidation()
        : base("DataSetKeyFieldConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
