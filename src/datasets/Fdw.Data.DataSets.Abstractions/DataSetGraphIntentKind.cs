using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The data set intends to read from another data set, which can itself be abstract.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphIntentKinds), "DataSet")]
public sealed class DataSetGraphIntentKind : GraphIntentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetGraphIntentKind"/> class.</summary>
    public DataSetGraphIntentKind() : base("DataSet", namesDataStore: false, namesDataSet: true)
    {
    }
}
