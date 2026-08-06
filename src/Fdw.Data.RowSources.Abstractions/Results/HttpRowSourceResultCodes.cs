using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.RowSources.Http.Abstractions.Results;

/// <summary>
/// TypeCollection for HTTP RowSource result codes.
/// Codes use categorized numbers (Id == EventId == number, Code == "HTTP-{number}").
/// </summary>
[TypeCollection(typeof(HttpRowSourceResultCodeBase), typeof(IResultCode), typeof(HttpRowSourceResultCodes))]
public abstract partial class HttpRowSourceResultCodes : TypeCollectionBase<HttpRowSourceResultCodeBase, IResultCode>
{
}

// =============================================================================
// HTTP Error Result Codes (category 7 — connection/execution band)
// =============================================================================