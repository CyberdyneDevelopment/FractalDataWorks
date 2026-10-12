using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Failed to create provider instance.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "ProviderCreationFailed")]
[ExcludeFromCodeCoverage]
public sealed class ProviderCreationFailedCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderCreationFailedCode"/> class.
    /// </summary>
    public ProviderCreationFailedCode()
        : base(91001, "ProviderCreationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to create provider: {Error}",
            isRetryable: false)
    {
    }
}