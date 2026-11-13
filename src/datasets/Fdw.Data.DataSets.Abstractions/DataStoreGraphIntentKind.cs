using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The data set intends to read from a data store, which can be external and not yet registered.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphIntentKinds), "DataStore")]
public sealed class DataStoreGraphIntentKind : GraphIntentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreGraphIntentKind"/> class.</summary>
    public DataStoreGraphIntentKind() : base("DataStore", namesDataStore: true, namesDataSet: false)
    {
    }
}
