using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Failed to persist execution item.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "ExecutionItemPersistFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecutionItemPersistFailedCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionItemPersistFailedCode"/> class.
    /// </summary>
    public ExecutionItemPersistFailedCode()
        : base(
            70002,
            "ExecutionItemPersistFailed",
            ResultSeverities.ByName("Error"),
            "Failed to persist execution item: {Error}",
            isRetryable: true)
    {
    }
}