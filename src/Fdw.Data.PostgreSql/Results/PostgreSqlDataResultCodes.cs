using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// TypeCollection for PostgreSQL Data command translator result codes.
/// EventId range: 5500-5599 (within Connections/Data 5000-5999)
/// </summary>
[TypeCollection(typeof(PostgreSqlDataResultCodeBase), typeof(IResultCode), typeof(PostgreSqlDataResultCodes))]
public abstract partial class PostgreSqlDataResultCodes : TypeCollectionBase<PostgreSqlDataResultCodeBase, IResultCode>
{
}

// =============================================================================
// Validation Result Codes (5500-5509)
// =============================================================================

// =============================================================================
// Compound Query Result Codes (5510-5519)
// =============================================================================

// =============================================================================
// Translation Exception Result Codes (5520-5549)
// =============================================================================
