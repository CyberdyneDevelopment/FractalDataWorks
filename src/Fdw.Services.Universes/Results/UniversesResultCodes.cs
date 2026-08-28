using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Universes.Results;

/// <summary>Result codes raised by the universes domain.</summary>
[TypeCollection(typeof(UniversesResultCodeBase), typeof(IResultCode), typeof(UniversesResultCodes))]
public abstract partial class UniversesResultCodes : TypeCollectionBase<UniversesResultCodeBase, IResultCode>
{
}
