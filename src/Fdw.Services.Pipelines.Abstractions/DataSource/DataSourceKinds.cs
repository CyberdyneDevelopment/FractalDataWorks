using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.DataSource;

/// <summary>
/// Collection of data source kind types.
/// </summary>
[TypeCollection(typeof(DataSourceKindBase), typeof(IDataSourceKind), typeof(DataSourceKinds))]
public abstract partial class DataSourceKinds : TypeCollectionBase<DataSourceKindBase, IDataSourceKind>
{
}
