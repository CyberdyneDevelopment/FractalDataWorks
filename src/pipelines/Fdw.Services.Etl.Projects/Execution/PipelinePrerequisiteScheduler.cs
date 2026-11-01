using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>
/// Runs the pipelines of one node in the order their prerequisites allow, never more than the stated number at once.
/// </summary>
/// <remarks>
/// A pipeline starts only when every pipeline it needs has succeeded. When a prerequisite fails, or is itself not run,
/// the pipelines that need it are never started. Nothing is assumed about a graph that cannot be run: a prerequisite that
/// names a pipeline outside the node, a cycle, or a limit below 1 fails the whole node with a logged message.
/// </remarks>
internal static class PipelinePrerequisiteScheduler
{
    private enum RunState
    {
        Waiting,
        Running,
        Succeeded,
        Failed,
        NotRun
    }

    /// <summary>Runs every pipeline once, honouring the prerequisites and the parallel limit.</summary>
    /// <param name="logger">The logger that records what the scheduler decided.</param>
    /// <param name="nodeName">The name of the node whose pipelines are run, for the log.</param>
    /// <param name="pipelineIds">The pipelines to run, in the order to prefer when several can start.</param>
    /// <param name="prerequisites">What each pipeline needs to have succeeded first.</param>
    /// <param name="maxParallelPipelines">The most pipelines that may run at once.</param>
    /// <param name="run">Runs one pipeline and returns whether it succeeded.</param>
    /// <param name="cancellationToken">Cancels the waiting; the token is passed to every run.</param>
    /// <returns>Success holding whether every pipeline succeeded, or the failure that says why the graph cannot be run.</returns>
    internal static async Task<IGenericResult<bool>> Execute(
        ILogger logger,
        string nodeName,
        IReadOnlyList<Guid> pipelineIds,
        IEnumerable<OrchestrationNodePipelinePrerequisiteConfiguration> prerequisites,
        int maxParallelPipelines,
        Func<Guid, CancellationToken, Task<bool>> run,
        CancellationToken cancellationToken = default)
    {
        if (maxParallelPipelines < 1)
        {
            return GenericResult<bool>.Failure(
                OrchestrationNodeOrchestratorLog.MaxParallelPipelinesInvalid(logger, nodeName, maxParallelPipelines));
        }

        var ids = pipelineIds.Distinct().ToList();
        var graph = CheckGraph(logger, nodeName, ids, prerequisites);
        if (graph.IsFailure)
        {
            return graph.ToNewResult<bool>();
        }

        var states = await Drive(logger, nodeName, ids, BuildNeeds(ids, prerequisites), maxParallelPipelines, run, cancellationToken)
            .ConfigureAwait(false);
        return GenericResult<bool>.Success(states.Values.All(s => s == RunState.Succeeded));
    }

    // Refuses a prerequisite outside the node and any cycle (the test the validator shares).
    private static IGenericResult CheckGraph(
        ILogger logger,
        string nodeName,
        List<Guid> ids,
        IEnumerable<OrchestrationNodePipelinePrerequisiteConfiguration> prerequisites)
    {
        var members = ids.ToHashSet();
        foreach (var prerequisite in prerequisites)
        {
            if (!members.Contains(prerequisite.PipelineId) || !members.Contains(prerequisite.PrerequisitePipelineId))
            {
                return GenericResult.Failure(
                    OrchestrationNodeOrchestratorLog.PrerequisiteOutsideNode(
                        logger, nodeName, prerequisite.PipelineId, prerequisite.PrerequisitePipelineId));
            }
        }

        var cyclic = PipelinePrerequisiteGraph.FindCyclic(ids, prerequisites);
        return cyclic.Count > 0
            ? GenericResult.Failure(OrchestrationNodeConfigurationLog.PrerequisiteCycleDetected(logger, nodeName, cyclic[0]))
            : GenericResult.Success();
    }

    // Maps each pipeline to the pipelines it needs; only called for a graph CheckGraph accepted, so every end is a member.
    private static Dictionary<Guid, List<Guid>> BuildNeeds(
        List<Guid> ids,
        IEnumerable<OrchestrationNodePipelinePrerequisiteConfiguration> prerequisites)
    {
        var needs = ids.ToDictionary(id => id, _ => new List<Guid>());
        foreach (var prerequisite in prerequisites)
        {
            needs[prerequisite.PipelineId].Add(prerequisite.PrerequisitePipelineId);
        }

        return needs;
    }

    // Starts what can start, waits for one to finish, repeats until nothing is running and nothing more can start.
    private static async Task<Dictionary<Guid, RunState>> Drive(
        ILogger logger,
        string nodeName,
        List<Guid> ids,
        Dictionary<Guid, List<Guid>> needs,
        int limit,
        Func<Guid, CancellationToken, Task<bool>> run,
        CancellationToken cancellationToken)
    {
        var states = ids.ToDictionary(id => id, _ => RunState.Waiting);
        var finished = Channel.CreateUnbounded<(Guid PipelineId, bool Succeeded)>();
        var started = new List<Task>();
        var running = 0;
        try
        {
            while (true)
            {
                MarkBlockedAsNotRun(logger, nodeName, ids, needs, states);
                foreach (var id in ids.Where(id => IsRunnable(id, needs, states)).Take(limit - running).ToList())
                {
                    states[id] = RunState.Running;
                    running++;
                    started.Add(RunOne(id, run, finished.Writer, cancellationToken));
                }

                if (running == 0)
                {
                    return states;
                }

                var done = await finished.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                running--;
                states[done.PipelineId] = done.Succeeded ? RunState.Succeeded : RunState.Failed;
            }
        }
        finally
        {
            await Task.WhenAll(started).ConfigureAwait(false);
        }
    }

    private static bool IsRunnable(Guid id, Dictionary<Guid, List<Guid>> needs, Dictionary<Guid, RunState> states) =>
        states[id] == RunState.Waiting && needs[id].All(need => states[need] == RunState.Succeeded);

    // A waiting pipeline whose prerequisite failed or was not run can never start; this carries that down the chain.
    private static void MarkBlockedAsNotRun(
        ILogger logger,
        string nodeName,
        List<Guid> ids,
        Dictionary<Guid, List<Guid>> needs,
        Dictionary<Guid, RunState> states)
    {
        bool changed;
        do
        {
            changed = false;
            foreach (var id in ids.Where(id => states[id] == RunState.Waiting))
            {
                var blockedBy = needs[id].Where(need => states[need] is RunState.Failed or RunState.NotRun).ToList();
                if (blockedBy.Count == 0)
                {
                    continue;
                }

                states[id] = RunState.NotRun;
                OrchestrationNodeOrchestratorLog.PipelineNotStartedPrerequisiteNotMet(logger, nodeName, id, blockedBy[0]);
                changed = true;
            }
        }
        while (changed);
    }

    // Runs one pipeline and always reports back, so the loop never waits for a run that threw.
    private static async Task RunOne(
        Guid id,
        Func<Guid, CancellationToken, Task<bool>> run,
        ChannelWriter<(Guid PipelineId, bool Succeeded)> finished,
        CancellationToken cancellationToken)
    {
        var succeeded = false;
        try
        {
            succeeded = await run(id, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            finished.TryWrite((id, succeeded));
        }
    }
}
