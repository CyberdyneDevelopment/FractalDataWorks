using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Trigger validation failed.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "TriggerValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TriggerValidationFailedCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerValidationFailedCode"/> class.
    /// </summary>
    public TriggerValidationFailedCode()
        : base(
            20002,
            "TriggerValidationFailed",
            ResultSeverities.ByName("Error"),
            "Trigger validation failed: {Reason}",
            isRetryable: false)
    {
    }
}