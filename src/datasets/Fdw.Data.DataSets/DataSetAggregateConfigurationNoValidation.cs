using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets;

/// <summary>States that <see cref="DataSetAggregateConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataSetAggregateConfiguration")]
public sealed class DataSetAggregateConfigurationNoValidation : UnvalidatedConfiguration<DataSetAggregateConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetAggregateConfigurationNoValidation"/> class.</summary>
    public DataSetAggregateConfigurationNoValidation()
        : base("DataSetAggregateConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
