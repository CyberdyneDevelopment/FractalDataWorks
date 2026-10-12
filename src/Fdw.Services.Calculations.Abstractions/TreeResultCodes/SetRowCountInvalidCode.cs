using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>A row count formula gave a value that is not a whole number within the limit.</summary>
[TypeOption(typeof(CalculationTreeResultCodes), "SetRowCountInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetRowCountInvalidCode : CalculationTreeResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="SetRowCountInvalidCode"/> class.</summary>
    public SetRowCountInvalidCode()
        : base(
            22026,
            "SetRowCountInvalid",
            ResultSeverities.ByName("Error"),
            "Output '{output}' refused: the row count is not a whole number from 0 up to the limit. {reason}",
            isRetryable: false)
    {
    }
}
