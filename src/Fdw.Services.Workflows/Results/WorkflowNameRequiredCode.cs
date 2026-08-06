using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workflows.Results;
/// <summary>
/// Result code for workflow name required.
/// </summary>
[TypeOption(typeof(WorkflowResultCodes), "WorkflowNameRequired")]
[ExcludeFromCodeCoverage]
public sealed class WorkflowNameRequiredCode : WorkflowResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref = "WorkflowNameRequiredCode"/> class.
    /// </summary>
    public WorkflowNameRequiredCode() : base(20000, "WorkflowNameRequired", ResultSeverities.ByName("Error"), "Workflow name is required", isRetryable: false)
    {
    }
}
