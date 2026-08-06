using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.UI.Providers.Results;

/// <summary>
/// TypeCollection of UI provider-context result codes.
/// </summary>
[TypeCollection(typeof(UIProviderResultCodeBase), typeof(IResultCode), typeof(UIProviderResultCodes))]
public abstract partial class UIProviderResultCodes : TypeCollectionBase<UIProviderResultCodeBase, IResultCode>
{
}
