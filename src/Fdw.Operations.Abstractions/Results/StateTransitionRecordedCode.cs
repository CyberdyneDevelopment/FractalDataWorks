using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// State transition recorded.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "StateTransitionRecorded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StateTransitionRecordedCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StateTransitionRecordedCode"/> class.
    /// </summary>
    public StateTransitionRecordedCode()
        : base(
            11004,
            "StateTransitionRecorded",
            ResultSeverities.ByName("Debug"),
            "Recorded state transition to '{NewState}'",
            isRetryable: false)
    {
    }
}