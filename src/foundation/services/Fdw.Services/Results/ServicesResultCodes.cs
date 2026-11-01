using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Results;

/// <summary>
/// TypeCollection for Services result codes.
/// Codes use the categorized-number scheme: Id == EventId == number, Code == "SERVICES-{number}".
/// </summary>
[TypeCollection(typeof(ServicesResultCodeBase), typeof(IResultCode), typeof(ServicesResultCodes))]
public abstract partial class ServicesResultCodes : TypeCollectionBase<ServicesResultCodeBase, IResultCode>
{
}