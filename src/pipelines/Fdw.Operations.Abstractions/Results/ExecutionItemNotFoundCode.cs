using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Execution item was not found.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "ExecutionItemNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecutionItemNotFoundCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionItemNotFoundCode"/> class.
    /// </summary>
    public ExecutionItemNotFoundCode()
        : base(
            31001,
            "ExecutionItemNotFound",
            ResultSeverities.ByName("Error"),
            "Execution item '{ExecutionItemId}' not found",
            isRetryable: false)
    {
    }
}