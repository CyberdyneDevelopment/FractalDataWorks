using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Interface for data source kinds.
/// </summary>
public interface IDataSourceKind : ITypeOption<int, DataSourceKindBase> { }
