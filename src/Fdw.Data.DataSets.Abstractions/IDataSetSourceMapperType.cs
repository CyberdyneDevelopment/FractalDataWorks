using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Represents a data set source mapper type definition.
/// Mapper types extract raw records from structured payloads (XML, JSON, CSV, etc.).
/// </summary>
public interface IDataSetSourceMapperType : ITypeOption<int, DataSetSourceMapperTypeBase>
{
}
