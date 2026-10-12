using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// TypeCollection for Data Service result codes.
/// Codes use categorized numbers (Id == EventId == number, Code == "DATA-{number}").
/// </summary>
[TypeCollection(typeof(DataServiceResultCodeBase), typeof(IResultCode), typeof(DataServiceResultCodes))]
public abstract partial class DataServiceResultCodes : TypeCollectionBase<DataServiceResultCodeBase, IResultCode>
{
}
