using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workflows.Results;

/// <summary>
/// Result code for generic data commands not supported by workflow services.
/// </summary>
[TypeOption(typeof(WorkflowResultCodes), "WorkflowCommandNotSupported")]
[ExcludeFromCodeCoverage]
public sealed class WorkflowCommandNotSupportedCode : WorkflowResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowCommandNotSupportedCode"/> class.
    /// </summary>
    public WorkflowCommandNotSupportedCode() : base(
        90004,
        "WorkflowCommandNotSupported",
        ResultSeverities.ByName("Error"),
        "Generic data commands are not supported by workflow services",
        isRetryable: false)
    {
    }
}
