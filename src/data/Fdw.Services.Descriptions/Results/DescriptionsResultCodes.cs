using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>Result codes raised by the descriptions domain.</summary>
[TypeCollection(typeof(DescriptionsResultCodeBase), typeof(IResultCode), typeof(DescriptionsResultCodes))]
public abstract partial class DescriptionsResultCodes : TypeCollectionBase<DescriptionsResultCodeBase, IResultCode>
{
}
