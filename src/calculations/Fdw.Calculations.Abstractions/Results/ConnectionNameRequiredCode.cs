using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Connection name is required for data retrieval.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "ConnectionNameRequired")]
[ExcludeFromCodeCoverage]
public sealed class ConnectionNameRequiredCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionNameRequiredCode"/> class.
    /// </summary>
    public ConnectionNameRequiredCode()
        : base(21002, "ConnectionNameRequired",
            ResultSeverities.ByName("Error"),
            "Connection name is required",
            isRetryable: false)
    {
    }
}