using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>
/// TypeCollection for the result codes of the calculation tree. Ids 22000-22019: validation band, unique within this collection.
/// </summary>
[TypeCollection(typeof(CalculationTreeResultCodeBase), typeof(ICalculationTreeResultCode), typeof(CalculationTreeResultCodes))]
public abstract partial class CalculationTreeResultCodes : TypeCollectionBase<CalculationTreeResultCodeBase, ICalculationTreeResultCode>
{
}
