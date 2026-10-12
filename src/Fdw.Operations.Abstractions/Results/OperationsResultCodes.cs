using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// TypeCollection for Operations result codes.
/// Codes use the categorized-number scheme (prefix "OPS"): Id == EventId == number, Code == "OPS-{number}".
/// </summary>
[TypeCollection(typeof(OperationsResultCodeBase), typeof(IResultCode), typeof(OperationsResultCodes))]
[ExcludeFromCodeCoverage]
public abstract partial class OperationsResultCodes : TypeCollectionBase<OperationsResultCodeBase, IResultCode>
{
}

// =============================================================================
// Execution Item Result Codes
// =============================================================================

// =============================================================================
// Escalation Result Codes
// =============================================================================

// =============================================================================
// Trigger Result Codes
// =============================================================================

// =============================================================================
// Correlation Result Codes
// =============================================================================