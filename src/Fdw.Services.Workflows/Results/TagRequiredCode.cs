using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workflows.Results;
/// <summary>
/// Result code for tag required.
/// </summary>
[TypeOption(typeof(WorkflowResultCodes), "TagRequired")]
[ExcludeFromCodeCoverage]
public sealed class TagRequiredCode : WorkflowResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref = "TagRequiredCode"/> class.
    /// </summary>
    public TagRequiredCode() : base(21000, "TagRequired", ResultSeverities.ByName("Error"), "Tag is required for workflow filtering", isRetryable: false)
    {
    }
}
