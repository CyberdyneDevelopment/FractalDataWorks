using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions.DataSource;

/// <summary>
/// Base class for data source kind types using CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class DataSourceKindBase : TypeOptionBase<int, DataSourceKindBase>, IDataSourceKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSourceKindBase"/> class.
    /// </summary>
    protected DataSourceKindBase(int id, string name) : base(id, name) { }
}
