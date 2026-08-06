using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Container name is required for data retrieval.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "ContainerNameRequired")]
[ExcludeFromCodeCoverage]
public sealed class ContainerNameRequiredCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNameRequiredCode"/> class.
    /// </summary>
    public ContainerNameRequiredCode()
        : base(21003, "ContainerNameRequired",
            ResultSeverities.ByName("Error"),
            "Container name is required",
            isRetryable: false)
    {
    }
}