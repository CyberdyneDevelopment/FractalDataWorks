using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// TypeCollection for DataSets domain result codes.
/// Codes use categorized catalog numbers (Code == "DATASETS-{number}", Id == EventId == number).
/// </summary>
[TypeCollection(typeof(DataSetsResultCodeBase), typeof(IResultCode), typeof(DataSetsResultCodes))]
public abstract partial class DataSetsResultCodes : TypeCollectionBase<DataSetsResultCodeBase, IResultCode>
{
}
