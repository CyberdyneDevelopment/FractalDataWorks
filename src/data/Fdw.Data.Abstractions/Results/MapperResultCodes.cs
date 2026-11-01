using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// TypeCollection for POCO-mapper result codes (category 9 / Internal — conversion failures).
/// </summary>
[TypeCollection(typeof(MapperResultCodeBase), typeof(IResultCode), typeof(MapperResultCodes))]
public abstract partial class MapperResultCodes : TypeCollectionBase<MapperResultCodeBase, IResultCode>
{
}
