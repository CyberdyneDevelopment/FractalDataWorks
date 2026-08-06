using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.DataSource.Kinds;

/// <summary>
/// Direct connection (ETL pattern) - reads directly from a physical connection.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSourceKinds), "Connection")]
public sealed class ConnectionKind : DataSourceKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionKind"/> class.
    /// </summary>
    public ConnectionKind() : base(1, "Connection") { }
}
