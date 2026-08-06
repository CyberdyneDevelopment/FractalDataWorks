using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workflows.Results;

/// <summary>
/// Result code for unexpected workflow service type in factory cast.
/// </summary>
[TypeOption(typeof(WorkflowResultCodes), "WorkflowUnexpectedServiceType")]
[ExcludeFromCodeCoverage]
public sealed class WorkflowUnexpectedServiceTypeCode : WorkflowResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowUnexpectedServiceTypeCode"/> class.
    /// </summary>
    public WorkflowUnexpectedServiceTypeCode() : base(
        90000,
        "WorkflowUnexpectedServiceType",
        ResultSeverities.ByName("Error"),
        "Unexpected workflow service type requested",
        isRetryable: false)
    {
    }
}
