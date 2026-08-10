using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Base class for data source kinds.
/// </summary>
public abstract class DataSourceKindBase : TypeOptionBase<int, DataSourceKindBase>, IDataSourceKind
{
    /// <summary>
    /// Initializes a new instance of <see cref="DataSourceKindBase"/>.
    /// </summary>
    protected DataSourceKindBase(int id, string name) : base(id, name) { }
}
