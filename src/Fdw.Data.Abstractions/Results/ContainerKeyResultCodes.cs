using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// TypeCollection for ContainerKey domain result codes.
/// Codes use categorized numbers (Id == EventId == number, Code == "CONTAINERKEY-{number}").
/// </summary>
[TypeCollection(typeof(ContainerKeyResultCodeBase), typeof(IResultCode), typeof(ContainerKeyResultCodes))]
public abstract partial class ContainerKeyResultCodes : TypeCollectionBase<ContainerKeyResultCodeBase, IResultCode>
{
}
