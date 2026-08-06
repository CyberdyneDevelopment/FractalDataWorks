using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// TypeCollection for data source kinds.
/// </summary>
[TypeCollection(typeof(DataSourceKindBase), typeof(IDataSourceKind), typeof(DataSourceKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class DataSourceKinds : TypeCollectionBase<DataSourceKindBase, IDataSourceKind> { }
