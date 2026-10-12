using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Types.MsSql;

/// <summary>
/// TypeCollection for Types MsSql result codes.
/// Codes use the categorized-number scheme: Id == EventId == number, Code == "TYPES-{number}",
/// and the handling category is number / 10000.
/// </summary>
[TypeCollection(typeof(MsSqlTypesResultCodeBase), typeof(IResultCode), typeof(MsSqlTypesResultCodes))]
public abstract partial class MsSqlTypesResultCodes : TypeCollectionBase<MsSqlTypesResultCodeBase, IResultCode>
{
}

