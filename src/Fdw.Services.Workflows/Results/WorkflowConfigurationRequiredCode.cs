using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workflows.Results;

/// <summary>
/// Result code for missing workflow configuration.
/// </summary>
[TypeOption(typeof(WorkflowResultCodes), "WorkflowConfigurationRequired")]
[ExcludeFromCodeCoverage]
public sealed class WorkflowConfigurationRequiredCode : WorkflowResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowConfigurationRequiredCode"/> class.
    /// </summary>
    public WorkflowConfigurationRequiredCode() : base(
        60000,
        "WorkflowConfigurationRequired",
        ResultSeverities.ByName("Error"),
        "Workflow configuration is required",
        isRetryable: false)
    {
    }
}
