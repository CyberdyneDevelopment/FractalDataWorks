using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.DataDestination.Kinds;

/// <summary>
/// Write directly to a connection (ETL pattern).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataDestinationKinds), "Connection")]
public sealed class ConnectionDestinationKind : DataDestinationKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionDestinationKind"/> class.
    /// </summary>
    public ConnectionDestinationKind() : base(1, "Connection") { }
}
