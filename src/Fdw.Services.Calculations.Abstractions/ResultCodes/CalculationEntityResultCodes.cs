using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.ResultCodes;

/// <summary>
/// TypeCollection for calculation entity result codes.
/// EventId range: 4140-4179 (Calculations domain)
/// </summary>
[TypeCollection(typeof(CalculationEntityResultCodeBase), typeof(ICalculationEntityResultCode), typeof(CalculationEntityResultCodes))]
public abstract partial class CalculationEntityResultCodes : TypeCollectionBase<CalculationEntityResultCodeBase, ICalculationEntityResultCode>
{
}
