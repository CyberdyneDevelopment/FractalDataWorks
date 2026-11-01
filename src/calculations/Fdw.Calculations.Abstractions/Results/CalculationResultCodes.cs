using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Calculations.Results;

/// <summary>
/// TypeCollection for Calculation result codes.
/// Codes use the categorized-number scheme: Id == EventId == number, Code == "CALC-{number}".
/// </summary>
[TypeCollection(typeof(CalculationResultCodeBase), typeof(IResultCode), typeof(CalculationResultCodes))]
public abstract partial class CalculationResultCodes : TypeCollectionBase<CalculationResultCodeBase, IResultCode>
{
}

