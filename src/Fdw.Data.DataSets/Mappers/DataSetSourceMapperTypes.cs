using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// TypeCollection for all data set source mapper type implementations.
/// Mappers extract raw records from structured payloads (XML, JSON, CSV, etc.).
/// Resolution: <c>DataSetSourceMapperTypes.ByName(sourceConfig.MapperTypeName)</c>.
/// </summary>
[TypeCollection(typeof(DataSetSourceMapperTypeBase), typeof(IDataSetSourceMapperType), typeof(DataSetSourceMapperTypes))]
[ExcludeFromCodeCoverage]
public sealed partial class DataSetSourceMapperTypes : TypeCollectionBase<DataSetSourceMapperTypeBase, IDataSetSourceMapperType>
{
    // TypeCollectionGenerator will generate all members
}
