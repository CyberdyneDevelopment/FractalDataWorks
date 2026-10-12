using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.ServiceTypes.Results;

/// <summary>
/// TypeCollection for ServiceType result codes.
/// Codes use categorized numbers (prefix "SERVICETYPE"); the number is the whole identity
/// (Id == EventId == number, Code == "SERVICETYPE-{number}") and its category is number / 10000.
/// </summary>
/// <remarks>
/// The numbers here are the SAME numbers as the failure log methods they pair with in
/// <c>ServiceTypeLog</c> — 61011 for the collection collect, 61012 for a single option. A phase failure
/// is reported once as a log message and once as a result code, and the shared number is what ties
/// the two records of one event together.
/// </remarks>
[TypeCollection(typeof(ServiceTypeResultCodeBase), typeof(IResultCode), typeof(ServiceTypeResultCodes))]
public abstract partial class ServiceTypeResultCodes : TypeCollectionBase<ServiceTypeResultCodeBase, IResultCode>
{
}
