using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.ResultCodes;

/// <summary>
/// Calculation entity configuration validation failed.
/// </summary>
[TypeOption(typeof(CalculationEntityResultCodes), "ValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ValidationFailedCode : CalculationEntityResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailedCode"/> class.
    /// </summary>
    public ValidationFailedCode()
        : base(
            20002,
            "ValidationFailed",
            ResultSeverities.ByName("Error"),
            "Calculation entity '{Name}' validation failed: {Reason}",
            isRetryable: false)
    {
    }
}
