using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Logging;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>
/// The organization an orchestration (project) run's own status broadcasts are addressed to (FDW-5986).
/// </summary>
/// <remarks>
/// An orchestration node has no owner of its own, so its organization is taken from the pipelines it runs, as a pipeline run's is taken from its pipeline (<see cref="OwningOrg"/>).
/// It is the organization the pipelines state when they state exactly one. Never one that is chosen or guessed: a node that runs no pipeline, whose pipelines state none, or whose
/// pipelines state more than one, has none, and each of those is said by its own coded message. A pipeline that cannot be read also leaves the run with none, since the pipelines read
/// before it could not speak for it. A run with none is still broadcast to its execution and pipeline groups, as it always was; only the organization's firehose does not receive it.
/// </remarks>
internal static class OrchestrationOwningOrg
{
    /// <summary>Reads the organization that owns the pipelines the node runs.</summary>
    /// <param name="root">The node whose run is announced.</param>
    /// <param name="pipelines">Reads the pipelines' configuration.</param>
    /// <param name="logger">The logger the reason a node has no organization is written to.</param>
    /// <param name="executionId">The run's execution item, for the log.</param>
    /// <param name="cancellationToken">A token that ends the reads.</param>
    /// <returns>The organization, or null when there is none to state.</returns>
    internal static async Task<Guid?> Of(
        IOrchestrationNodeImplementationConfiguration root, IPipelineConfigurationProvider pipelines, ILogger logger, Guid executionId, CancellationToken cancellationToken)
    {
        var pipelineIds = PipelineIdsOf(root).Distinct().ToList();
        if (pipelineIds.Count == 0)
        {
            OrchestrationNodeOrchestratorLog.RunOrganizationNoPipeline(logger, executionId, root.Name);
            return null;
        }

        var organizations = new HashSet<Guid>();
        foreach (var pipelineId in pipelineIds)
        {
            var owner = await OwningOrg.Of(pipelines, pipelineId, cancellationToken).ConfigureAwait(false);
            if (owner.IsFailure)
            {
                OrchestrationNodeOrchestratorLog.RunOrganizationPipelineNotRead(logger, executionId, root.Name, pipelineId, owner.CurrentMessage);
                return null;
            }

            if (owner.Value is { } organization)
            {
                _ = organizations.Add(organization);
            }
        }

        if (organizations.Count == 0)
        {
            OrchestrationNodeOrchestratorLog.RunOrganizationNoneStated(logger, executionId, root.Name, pipelineIds.Count);
            return null;
        }

        if (organizations.Count > 1)
        {
            OrchestrationNodeOrchestratorLog.RunOrganizationSeveral(logger, executionId, root.Name, organizations.Count);
            return null;
        }

        return organizations.Single();
    }

    // The pipelines of the node and of every node below it.
    private static IEnumerable<Guid> PipelineIdsOf(IOrchestrationNodeImplementationConfiguration node) =>
        node.PipelineMemberships.Select(membership => membership.PipelineId).Concat(node.Children.SelectMany(child => PipelineIdsOf(child)));
}
