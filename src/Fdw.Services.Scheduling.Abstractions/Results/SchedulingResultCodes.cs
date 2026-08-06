using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// TypeCollection for Scheduling result codes.
/// Categorized number scheme: codes carry a category-banded number (e.g. 20000 InvalidInput band).
/// </summary>
[TypeCollection(typeof(SchedulingResultCodeBase), typeof(IResultCode), typeof(SchedulingResultCodes))]
public abstract partial class SchedulingResultCodes : TypeCollectionBase<SchedulingResultCodeBase, IResultCode>
{
}

// =============================================================================
// Trigger Validation Result Codes
// =============================================================================