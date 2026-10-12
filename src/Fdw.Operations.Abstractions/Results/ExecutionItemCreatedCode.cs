using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// Created execution item.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "ExecutionItemCreated", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecutionItemCreatedCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionItemCreatedCode"/> class.
    /// </summary>
    public ExecutionItemCreatedCode()
        : base(
            11003,
            "ExecutionItemCreated",
            ResultSeverities.ByName("Information"),
            "Created execution item '{Name}' ({ItemType})",
            isRetryable: false)
    {
    }
}