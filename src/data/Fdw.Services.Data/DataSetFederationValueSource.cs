using Fdw.Collections.Attributes;

namespace Fdw.Services.Data;

/// <summary>The data set's own value (its FederationOptions, or the strategy it states).</summary>
[TypeOption(typeof(FederationValueSources), "DataSet")]
public sealed class DataSetFederationValueSource : FederationValueSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetFederationValueSource"/> class.</summary>
    public DataSetFederationValueSource()
        : base("DataSet")
    {
    }
}
