using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.DataSource.Kinds;

/// <summary>
/// Logical dataset (ELT pattern) - reads from a pre-defined logical dataset.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSourceKinds), "DataSet")]
public sealed class DataSetKind : DataSourceKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetKind"/> class.
    /// </summary>
    public DataSetKind() : base(2, "DataSet") { }
}
