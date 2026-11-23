using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>A named data set.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComputeTableSources), "DataSet")]
public sealed class DataSetComputeTableSource : ComputeTableSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetComputeTableSource"/> class.</summary>
    public DataSetComputeTableSource()
        : base("DataSet", true, "a data set or output source states its name")
    {
    }
}
