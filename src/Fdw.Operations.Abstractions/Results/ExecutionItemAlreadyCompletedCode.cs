using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Execution item is already completed.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "ExecutionItemAlreadyCompleted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecutionItemAlreadyCompletedCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionItemAlreadyCompletedCode"/> class.
    /// </summary>
    public ExecutionItemAlreadyCompletedCode()
        : base(
            40001,
            "ExecutionItemAlreadyCompleted",
            ResultSeverities.ByName("Warning"),
            "Execution item '{ExecutionItemId}' is already completed",
            isRetryable: false)
    {
    }
}