using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.StateCollections.Results;

/// <summary>
/// TypeCollection for state-machine result codes.
/// Codes use categorized numbers (Id == EventId == number, Code == "SM-{number}").
/// </summary>
[TypeCollection(typeof(StateMachineResultCodeBase), typeof(IResultCode), typeof(StateMachineResultCodes))]
public abstract partial class StateMachineResultCodes : TypeCollectionBase<StateMachineResultCodeBase, IResultCode>
{
}
