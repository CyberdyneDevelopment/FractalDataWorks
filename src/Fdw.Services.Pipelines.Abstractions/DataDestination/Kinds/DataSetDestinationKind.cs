using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.DataDestination.Kinds;

/// <summary>
/// Produce a dataset (ELT pattern) - can be consumed by other pipelines or APIs.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataDestinationKinds), "DataSet")]
public sealed class DataSetDestinationKind : DataDestinationKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetDestinationKind"/> class.
    /// </summary>
    public DataSetDestinationKind() : base(2, "DataSet") { }
}
