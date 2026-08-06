using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workflows.Results;

/// <summary>
/// Result code for invalid configuration type passed to workflow factory.
/// </summary>
[TypeOption(typeof(WorkflowResultCodes), "WorkflowInvalidConfigurationType")]
[ExcludeFromCodeCoverage]
public sealed class WorkflowInvalidConfigurationTypeCode : WorkflowResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowInvalidConfigurationTypeCode"/> class.
    /// </summary>
    public WorkflowInvalidConfigurationTypeCode() : base(
        60003,
        "WorkflowInvalidConfigurationType",
        ResultSeverities.ByName("Error"),
        "Invalid configuration type: expected WorkflowConfiguration",
        isRetryable: false)
    {
    }
}
