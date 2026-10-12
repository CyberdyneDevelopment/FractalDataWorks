using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Trigger accepted and execution created.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "TriggerAccepted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TriggerAcceptedCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerAcceptedCode"/> class.
    /// </summary>
    public TriggerAcceptedCode()
        : base(
            11005,
            "TriggerAccepted",
            ResultSeverities.ByName("Information"),
            "Trigger accepted, execution '{ExecutionId}' created",
            isRetryable: false)
    {
    }
}