using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Execution item name is required.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "ExecutionItemNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecutionItemNameRequiredCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionItemNameRequiredCode"/> class.
    /// </summary>
    public ExecutionItemNameRequiredCode()
        : base(
            20000,
            "ExecutionItemNameRequired",
            ResultSeverities.ByName("Error"),
            "Execution item name is required",
            isRetryable: false)
    {
    }
}