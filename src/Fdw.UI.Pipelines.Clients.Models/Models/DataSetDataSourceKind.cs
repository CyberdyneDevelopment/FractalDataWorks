using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>Logical dataset (ELT pattern) - reads from a pre-defined logical dataset.</summary>
[TypeOption(typeof(DataSourceKinds), "DataSet")]
[ExcludeFromCodeCoverage]
public sealed class DataSetDataSourceKind : DataSourceKindBase
{
    /// <summary>Initializes a new instance of <see cref="DataSetDataSourceKind"/>.</summary>
    public DataSetDataSourceKind() : base(2, "DataSet") { }
}
