using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Produce a dataset (ELT pattern) - can be consumed by other pipelines or APIs.</summary>
[TypeOption(typeof(DataDestinationKinds), "DataSet")]
[ExcludeFromCodeCoverage]
public sealed class DataSetDataDestinationKind : DataDestinationKindBase
{
    /// <summary>Initializes a new instance of <see cref="DataSetDataDestinationKind"/>.</summary>
    public DataSetDataDestinationKind() : base(2, "DataSet") { }
}
