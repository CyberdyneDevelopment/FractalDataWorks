using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.ResultCodes;

/// <summary>
/// An abstract DataSet cannot be used as a calculation input.
/// </summary>
[TypeOption(typeof(CalculationEntityResultCodes), "AbstractDataSetCannotBeUsedAsInput", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AbstractDataSetCannotBeUsedAsInputCode : CalculationEntityResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractDataSetCannotBeUsedAsInputCode"/> class.
    /// </summary>
    public AbstractDataSetCannotBeUsedAsInputCode()
        : base(
            20001,
            "AbstractDataSetCannotBeUsedAsInput",
            ResultSeverities.ByName("Error"),
            "Abstract DataSets cannot be used as calculation inputs; a concrete DataSet is required",
            isRetryable: false)
    {
    }
}
