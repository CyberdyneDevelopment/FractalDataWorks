using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions.DataSource;

/// <summary>
/// Interface for data source kind types.
/// </summary>
public interface IDataSourceKind : ITypeOption<int, DataSourceKindBase>
{
}
