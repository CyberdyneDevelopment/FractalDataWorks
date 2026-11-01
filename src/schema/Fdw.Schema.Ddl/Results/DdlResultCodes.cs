#pragma warning disable CS1591
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Schema.Ddl.Results;

/// <summary>
/// Result codes for DDL generation operations.
/// </summary>
[TypeCollection(typeof(DdlResultCodeBase), typeof(IResultCode), typeof(DdlResultCodes))]
public abstract partial class DdlResultCodes : TypeCollectionBase<DdlResultCodeBase, IResultCode>
{
}