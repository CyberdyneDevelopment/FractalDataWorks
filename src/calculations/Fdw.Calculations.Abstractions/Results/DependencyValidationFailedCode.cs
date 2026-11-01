using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Dependency validation failed - missing required datasets and/or calculations.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "DependencyValidationFailed")]
[ExcludeFromCodeCoverage]
public sealed class DependencyValidationFailedCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyValidationFailedCode"/> class.
    /// </summary>
    public DependencyValidationFailedCode()
        : base(20001, "DependencyValidationFailed",
            ResultSeverities.ByName("Error"),
            "Dependency validation failed: {Errors}",
            isRetryable: false)
    {
    }
}