using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.UI.Rendering.Blazor.Results;

/// <summary>
/// TypeCollection of Blazor UI rendering result codes.
/// </summary>
[TypeCollection(typeof(BlazorUIResultCodeBase), typeof(IResultCode), typeof(BlazorUIResultCodes))]
public abstract partial class BlazorUIResultCodes : TypeCollectionBase<BlazorUIResultCodeBase, IResultCode>
{
}
