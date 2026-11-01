using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Tds.Endpoints.Results;

/// <summary>Result codes raised by the SQL Server endpoint.</summary>
[TypeCollection(typeof(TdsEndpointResultCodeBase), typeof(IResultCode), typeof(TdsEndpointResultCodes))]
public abstract partial class TdsEndpointResultCodes : TypeCollectionBase<TdsEndpointResultCodeBase, IResultCode>
{
}
