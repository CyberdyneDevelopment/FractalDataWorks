using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// TypeCollection for Aegis Gateway result codes.
/// Codes use categorized catalog numbers (prefix "AEG"; Code == "AEG-{number}", Id == EventId == number).
/// </summary>
[TypeCollection(typeof(AegisResultCodeBase), typeof(IResultCode), typeof(AegisResultCodes))]
public abstract partial class AegisResultCodes : TypeCollectionBase<AegisResultCodeBase, IResultCode>
{
}
