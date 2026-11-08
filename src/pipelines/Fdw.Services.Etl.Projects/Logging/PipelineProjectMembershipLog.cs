using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Logging;

/// <summary>MessageLogging for the read of which projects run each pipeline (FDW-4449).</summary>
[MessageLoggingTypeCode("PROJECTS")]
public static partial class PipelineProjectMembershipLog
{
    /// <summary>Logs steps that run pipelines but sit under no project, which the read leaves out.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="stepCount">How many nodes with pipeline memberships have no project above them.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why Information and not a failure: a step with no project above it is a fault in the stored tree, not in the caller's request; the other pipelines still read.
    [MessageLogging(
        EventId = 61156,
        Level = LogLevel.Information,
        Message = "{stepCount} orchestration node(s) run pipelines but have no project above them; their pipelines are left out of the project memberships")]
    public static partial IGenericMessage NodesWithoutProject(ILogger logger, int stepCount);
}
