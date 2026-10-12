using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.DataSource.Kinds;

/// <summary>
/// Data store container (ETL pattern): reads the rows of one container of a configured data store, addressed by
/// store, path and container as a <c>DataStoreTarget</c> is (FDW-1103).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSourceKinds), "DataStore")]
public sealed class DataStoreKind : DataSourceKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreKind"/> class.
    /// </summary>
    public DataStoreKind() : base("DataStore") { }
}
