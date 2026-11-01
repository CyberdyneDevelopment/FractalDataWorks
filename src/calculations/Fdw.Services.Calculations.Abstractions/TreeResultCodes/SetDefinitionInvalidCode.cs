using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.TreeResultCodes;

/// <summary>A calculation set definition is invalid.</summary>
[TypeOption(typeof(CalculationTreeResultCodes), "SetDefinitionInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetDefinitionInvalidCode : CalculationTreeResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="SetDefinitionInvalidCode"/> class.</summary>
    public SetDefinitionInvalidCode()
        : base(
            22027,
            "SetDefinitionInvalid",
            ResultSeverities.ByName("Error"),
            "Calculation set '{output}' refused: the definition is invalid. {reason}",
            isRetryable: false)
    {
    }
}
