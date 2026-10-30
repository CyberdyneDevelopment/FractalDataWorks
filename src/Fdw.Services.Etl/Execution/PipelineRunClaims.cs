using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Works out what one pipeline run needs from the scheduler: the tables and files it writes (FDW-3085, FDW-3193).
/// </summary>
/// <remarks>
/// <para>
/// What a run writes is read from the pipeline by <see cref="GraphPipelineUses"/>, the one way the workspace reads it. A destination that
/// names a container writes that container; a destination that names a data set writes the container of the data set's one source. Both
/// are resolved by <see cref="LoadTargetResolver"/>, the one rule the load itself uses, and a table is named by its connection, path and
/// container, so two data stores on one connection that address one table are one key. A test run writes nothing.
/// </para>
/// <para>
/// Every claim of a graph pipeline also carries the pipeline's cap (FDW-3117): how many of its runs may be in flight at once, and whether a
/// run past that waits its turn, is refused or is held until a set time (FDW-3129), all read from the pipeline. A pipeline that states neither in a usable way fails the run,
/// and no number or behavior is assumed.
/// </para>
/// <para>
/// A run whose writes cannot be told runs alone: a pipeline that is not a graph, one whose uses cannot be read, one that runs SQL against a
/// data store (what a SQL step writes is not in the pipeline), and a data set that does not resolve to one container. No other assumption
/// is made about such a run.
/// </para>
/// </remarks>
public static class PipelineRunClaims
{
    private static readonly IReadOnlyDictionary<string, object?> NoParameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Works out the claim of a run.</summary>
    /// <param name="request">The run.</param>
    /// <param name="configuration">The pipeline, read whole; <see langword="null"/> when it could not be read.</param>
    /// <param name="targets">Resolves a data set or a container to the table it addresses.</param>
    /// <param name="limits">The limits read for the run.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The claim, or the failure naming the pipeline and the value it does not state.</returns>
    public static async Task<IGenericResult<PipelineRunClaim>> For(
        PipelineExecutionRequest request,
        IPipelineImplementationConfiguration? configuration,
        LoadTargetResolver targets,
        PipelineRunLimitValues limits,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(logger);

        var run = new PipelineRunIdentity(request.ExecutionId, request.PipelineId, request.PipelineName, request.TenantId);
        var graph = configuration as GraphPipelineConfiguration;
        PipelineRunCap? cap = null;
        if (graph is not null)
        {
            var found = CapOf(request, graph, logger);
            if (found.IsFailure || found.Value is null)
            {
                return found.ToNewResult<PipelineRunClaim>();
            }

            cap = found.Value;
        }

        if (request.TestRowsPerSource is not null)
        {
            return GenericResult<PipelineRunClaim>.Success(PipelineRunClaim.Writing(run, [], limits, cap));
        }

        if (graph is null)
        {
            return GenericResult<PipelineRunClaim>.Success(Alone(run, limits, cap, logger, "its pipeline is not a graph pipeline that could be read"));
        }

        // A run that was given no parameters reads its uses with none; it is not given values.
        var uses = GraphPipelineUses.Of(graph, request.Parameters is { } supplied ? supplied : NoParameters, logger);
        if (uses.IsFailure || uses.Value is null)
        {
            return GenericResult<PipelineRunClaim>.Success(Alone(run, limits, cap, logger, "what the pipeline uses could not be read for these values"));
        }

        if (uses.Value.FirstOrDefault(use => IsSqlStep(use)) is { } sql)
        {
            return GenericResult<PipelineRunClaim>.Success(
                Alone(run, limits, cap, logger, $"it runs SQL against data store '{sql.Name}', and what a SQL step writes is not in the pipeline"));
        }

        var keys = new List<string>();
        foreach (var use in uses.Value.Where(use => IsWrite(use)))
        {
            var key = await KeyOf(request, use, targets, logger, cancellationToken).ConfigureAwait(false);
            if (key is null)
            {
                return GenericResult<PipelineRunClaim>.Success(
                    Alone(run, limits, cap, logger, $"{use.Kind} '{use.Name}' that it writes does not resolve to one container"));
            }

            keys.Add(key);
        }

        return GenericResult<PipelineRunClaim>.Success(PipelineRunClaim.Writing(run, keys, limits, cap));
    }

    // The pipeline is named by its id; runs of it count together whatever its name is at the time.
    private static IGenericResult<PipelineRunCap> CapOf(PipelineExecutionRequest request, GraphPipelineConfiguration graph, ILogger logger)
    {
        if (graph.MaxParallelRuns < 1)
        {
            return GenericResult<PipelineRunCap>.Failure(
                PipelineRunSchedulerLog.ParallelismInvalid(logger, request.PipelineName, "maxParallelRuns", $"{graph.MaxParallelRuns} is not at least one"));
        }

        var behavior = OverflowBehaviors.ByName(graph.OverflowBehavior);
        if (ReferenceEquals(behavior, OverflowBehaviors.NotFound))
        {
            return GenericResult<PipelineRunCap>.Failure(PipelineRunSchedulerLog.ParallelismInvalid(
                logger, request.PipelineName, "overflowBehavior", $"'{graph.OverflowBehavior}' is not one of {nameof(OverflowBehaviors)}"));
        }

        PipelineDelayedStart? delay = null;
        if (behavior.HoldsWhenFull)
        {
            var problems = PipelineDelayedStart.TryRead(graph.DelayedStartSeconds, graph.DelayedStartTimeOfDay, graph.DelayedStartTimeZoneId, out delay);
            if (problems.Count > 0)
            {
                return GenericResult<PipelineRunCap>.Failure(PipelineRunSchedulerLog.ParallelismInvalid(
                    logger, request.PipelineName, "delayedStart", string.Join("; ", problems.Select(problem => problem.Message))));
            }
        }

        return GenericResult<PipelineRunCap>.Success(new PipelineRunCap(string.Concat("pipeline/", request.PipelineId), graph.MaxParallelRuns, behavior.RefusesWhenFull, delay));
    }

    private static bool IsSqlStep(PipelineUse use) =>
        string.Equals(use.Relation, PipelineUseRelations.Uses, StringComparison.Ordinal)
        && string.Equals(use.Kind, PipelineUseKinds.DataStore, StringComparison.Ordinal);

    private static bool IsWrite(PipelineUse use) => string.Equals(use.Relation, PipelineUseRelations.Writes, StringComparison.Ordinal);

    private static async Task<string?> KeyOf(
        PipelineExecutionRequest request, PipelineUse use, LoadTargetResolver targets, ILogger logger, CancellationToken cancellationToken)
    {
        var owner = string.Concat("the claim of pipeline '", request.PipelineName, "'");
        var resolved = use.Kind switch
        {
            PipelineUseKinds.Container => await targets.ForContainer(
                owner, use.Parents.Count > 0 ? use.Parents[0] : string.Empty, use.Parents.Count > 1 ? use.Parents[1] : string.Empty, use.Name, cancellationToken)
                .ConfigureAwait(false),
            PipelineUseKinds.DataSet => await targets.ForDataSet(owner, use.Name, cancellationToken).ConfigureAwait(false),
            _ => null
        };
        if (resolved is not { IsSuccess: true, Value: { } target })
        {
            return null;
        }

        // A destination that writes one file claims that file (FDW-3193); one that writes files split by a field claims the whole container, since the files are
        // known only when the rows arrive.
        if (string.IsNullOrWhiteSpace(use.PhysicalPath) && string.IsNullOrWhiteSpace(use.PhysicalName))
        {
            return Key(target);
        }

        var moved = target.Readdressed(owner, logger, use.PhysicalPath, use.PhysicalName);
        return moved is { IsSuccess: true, Value: { } file } ? string.Concat(Key(file), "@", file.Container.Path.PathValue) : null;
    }

    // The same table is the same key however it was named: the connection, the path and the container, without regard to case.
    private static string Key(LoadTarget target) => string.Concat(target.ConnectionId, "/", target.Container.Parent.Name, "/", target.Container.Name);

    private static PipelineRunClaim Alone(PipelineRunIdentity run, PipelineRunLimitValues limits, PipelineRunCap? cap, ILogger logger, string reason)
    {
        PipelineRunSchedulerLog.RunMustBeAlone(logger, run.PipelineName, reason);
        return PipelineRunClaim.RunningAlone(run, limits, cap);
    }
}
