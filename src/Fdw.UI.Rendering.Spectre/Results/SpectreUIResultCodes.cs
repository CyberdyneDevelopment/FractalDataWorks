using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.UI.Rendering.Spectre.Results;

/// <summary>
/// TypeCollection for Spectre UI result codes.
/// Codes use the categorized-number scheme (Id == EventId == number, Code == "SPECTRE-{number}").
/// </summary>
[TypeCollection(typeof(SpectreUIResultCodeBase), typeof(IResultCode), typeof(SpectreUIResultCodes))]
public abstract partial class SpectreUIResultCodes : TypeCollectionBase<SpectreUIResultCodeBase, IResultCode>
{
}

// =============================================================================
// Render Context Validation Result Codes (category 2 — validation)
// =============================================================================