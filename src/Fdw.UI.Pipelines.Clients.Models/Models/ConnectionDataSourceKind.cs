using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Direct connection (ETL pattern) - reads directly from a physical connection.</summary>
[TypeOption(typeof(DataSourceKinds), "Connection")]
[ExcludeFromCodeCoverage]
public sealed class ConnectionDataSourceKind : DataSourceKindBase
{
    /// <summary>Initializes a new instance of <see cref="ConnectionDataSourceKind"/>.</summary>
    public ConnectionDataSourceKind() : base(1, "Connection") { }
}
