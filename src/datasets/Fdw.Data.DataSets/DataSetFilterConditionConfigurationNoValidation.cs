using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="DataSetFilterConditionConfiguration"/> has no validator of its own, and why (FDW-4997).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "DataSetFilterConditionConfiguration")]
public sealed class DataSetFilterConditionConfigurationNoValidation : UnvalidatedConfiguration<DataSetFilterConditionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetFilterConditionConfigurationNoValidation"/> class.</summary>
    public DataSetFilterConditionConfigurationNoValidation()
        : base("DataSetFilterConditionConfiguration", "A filter condition is checked against the data set that owns it, not on its own: its field must be one of that data set's, its operator a filter operator and its value readable as its data type. Those rules run over the whole data set when it is created or updated and again when it is read (DataSetFilters, FDW-4997), and a condition that breaks one is refused with its own code.")
    {
    }
}
