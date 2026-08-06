using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Write directly to a connection (ETL pattern).</summary>
[TypeOption(typeof(DataDestinationKinds), "Connection")]
[ExcludeFromCodeCoverage]
public sealed class ConnectionDataDestinationKind : DataDestinationKindBase
{
    /// <summary>Initializes a new instance of <see cref="ConnectionDataDestinationKind"/>.</summary>
    public ConnectionDataDestinationKind() : base(1, "Connection") { }
}
