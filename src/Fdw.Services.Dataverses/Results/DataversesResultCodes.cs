using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Dataverses.Results;

/// <summary>Result codes raised by the dataverses domain.</summary>
[TypeCollection(typeof(DataversesResultCodeBase), typeof(IResultCode), typeof(DataversesResultCodes))]
public abstract partial class DataversesResultCodes : TypeCollectionBase<DataversesResultCodeBase, IResultCode>
{
}
