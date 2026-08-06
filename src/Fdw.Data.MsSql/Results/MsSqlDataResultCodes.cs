using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// TypeCollection for MsSql Data command translator result codes.
/// EventId range: 5400-5499 (within Connections/Data 5000-5999)
/// </summary>
[TypeCollection(typeof(MsSqlDataResultCodeBase), typeof(IResultCode), typeof(MsSqlDataResultCodes))]
public abstract partial class MsSqlDataResultCodes : TypeCollectionBase<MsSqlDataResultCodeBase, IResultCode>
{
}

// =============================================================================
// Validation Result Codes (5400-5409)
// =============================================================================

// =============================================================================
// Compound Query Result Codes (5410-5419)
// =============================================================================

// =============================================================================
// Translation Exception Result Codes (5420-5449)
// =============================================================================