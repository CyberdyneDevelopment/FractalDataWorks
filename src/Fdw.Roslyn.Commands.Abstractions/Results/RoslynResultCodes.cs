using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// TypeCollection for Roslyn command result codes.
/// Each code's identity is a categorized number (Id == EventId == number, Code == "ROSLYN-{number}").
/// </summary>
[TypeCollection(typeof(RoslynResultCodeBase), typeof(IResultCode), typeof(RoslynResultCodes))]
public abstract partial class RoslynResultCodes : TypeCollectionBase<RoslynResultCodeBase, IResultCode>
{
}
