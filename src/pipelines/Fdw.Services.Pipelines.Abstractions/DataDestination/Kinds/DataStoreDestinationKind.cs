using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.DataDestination.Kinds;

/// <summary>
/// Data store container (ETL pattern): writes the rows into one container of a configured data store, addressed by
/// store, path and container as a <c>DataStoreTarget</c> is (FDW-1269).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataDestinationKinds), "DataStore")]
public sealed class DataStoreDestinationKind : DataDestinationKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreDestinationKind"/> class.
    /// </summary>
    public DataStoreDestinationKind() : base("DataStore") { }
}
