using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workflows.Results;
// ============================================================
// Result Code Implementations
// ============================================================
/// <summary>
/// Result code for workflow not found.
/// </summary>
[TypeOption(typeof(WorkflowResultCodes), "WorkflowNotFound")]
[ExcludeFromCodeCoverage]
public sealed class WorkflowNotFoundCode : WorkflowResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref = "WorkflowNotFoundCode"/> class.
    /// </summary>
    public WorkflowNotFoundCode() : base(30000, "WorkflowNotFound", ResultSeverities.ByName("Warning"), "Workflow not found", isRetryable: false)
    {
    }
}
