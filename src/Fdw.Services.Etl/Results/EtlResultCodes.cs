using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// TypeCollection for ETL result codes.
/// EventId range: 8101-8199 (Services.Etl domain)
/// </summary>
[TypeCollection(typeof(EtlResultCodeBase), typeof(IResultCode), typeof(EtlResultCodes))]
public abstract partial class EtlResultCodes : TypeCollectionBase<EtlResultCodeBase, IResultCode>
{
}

// =============================================================================
// Transform Result Codes (8170-8189)
// =============================================================================