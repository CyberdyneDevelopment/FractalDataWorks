using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Predicates;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Routes each record down the first of its paths whose condition it meets, runs that path's stages on the records it took, and
/// joins the paths' outputs.
/// </summary>
/// <remarks>
/// FDW-1120. The outputs follow one another in path order, then the records that matched no path when <c>Pass</c> lets them
/// on; the order of records across paths is not the order they arrived in. A path's stages are ordinary stages and may be
/// Branch stages themselves, down to <see cref="MaxDepth"/>. A stage that needs the whole input (an Aggregate) inside a path
/// sees the whole path's records, because the pipeline stops streaming when any stage below a Branch needs it.
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "Branch")]
public sealed class BranchTransformType : TransformTypeBase, ICompositeTransformType, INestedStageShape
{
    private const string Kind = "Branch";

    private static readonly StageMember[] OwnedMembers =
    [
        new("paths", "PathList", true, "The paths of the branch, each a name, a condition and the stages a record that takes the path passes through; the first path whose condition a record meets takes it"),
        new("unmatched", "UnmatchedAction", true, "What a record that meets no path causes: Pass lets it on, Reject drops it and reports it")
    ];

    /// <summary>Initializes a new instance of the <see cref="BranchTransformType"/> class.</summary>
    public BranchTransformType() : base(
        name: Kind,
        displayName: "Branch",
        description: "Sends each record down the first path whose condition it meets, with its own stages per path",
        category: "Routing",
        modifiesStructure: false,
        canFilterRecords: true,
        inputPorts: ["input"],
        outputPorts: ["output"])
    {
    }

    /// <inheritdoc />
    public int MaxDepth => 3;

    /// <inheritdoc />
    public override IReadOnlyList<StageMember> Members => OwnedMembers;

    /// <inheritdoc />
    public override async Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        var routed = await TransformBatch([input], configuration, context, cancellationToken).ConfigureAwait(false);
        if (!routed.IsSuccess)
        {
            return routed.ToNewResult<IDictionary<string, object?>>();
        }

        var produced = routed.Value!.ToList();
        if (produced.Count > 1)
        {
            return GenericResult<IDictionary<string, object?>>.Failure(TransformLog.TransformParamsInvalid(
                context.Logger, configuration is PipelineTransformConfiguration config ? config.Name : Kind, Kind, "a path of a branch produced more than one record from one record, which a per-record run cannot carry"));
        }

        return GenericResult<IDictionary<string, object?>>.Success(produced.Count == 0 ? null! : produced[0]);
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, Kind, configuration.GetType().Name));
        }

        var prepared = BranchPaths.Prepare(config, context);
        if (prepared.IsFailure)
        {
            return prepared.ToNewResult<IEnumerable<IDictionary<string, object?>>>();
        }

        var branch = prepared.Value!;
        var taken = branch.Paths.Select(_ => new List<IDictionary<string, object?>>()).ToList();
        var unmatched = new List<IDictionary<string, object?>>();
        var inCount = 0;
        foreach (var record in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            inCount++;
            var index = branch.FirstMatch(record);
            (index < 0 ? unmatched : taken[index]).Add(record);
        }

        var output = new List<IDictionary<string, object?>>();
        for (var index = 0; index < branch.Paths.Count; index++)
        {
            var produced = await StageRunner.RunBatch(branch.Paths[index].Path.Stages, taken[index], context, cancellationToken).ConfigureAwait(false);
            if (!produced.IsSuccess)
            {
                return produced.ToNewResult<IEnumerable<IDictionary<string, object?>>>();
            }

            output.AddRange(produced.Value!);
        }

        PassOrReject(branch, config, unmatched, context, output);
        TransformLog.BranchApplied(
            context.Logger, config.Name, inCount, string.Join(", ", branch.Paths.Select((entry, index) => string.Concat(entry.Path.Name, "=", taken[index].Count))), unmatched.Count);
        return GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(output);
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (spec is not ITransformBranchingSpec branching)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, "the request does not state paths"));
        }

        var problem = FirstProblem(branching);
        if (problem is not null)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, problem));
        }

        foreach (var path in branching.Paths)
        {
            var read = RecordPredicates.Parse(path.Condition, logger);
            if (read.IsFailure)
            {
                return read;
            }
        }

        config.Unmatched = branching.Unmatched;
        config.Paths = branching.Paths
            .Select((path, index) => new PipelineTransformPathConfiguration { Name = path.Name, Condition = path.Condition, Ordinal = index + 1 })
            .ToList();

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public IReadOnlyList<ITransformPathSpec> PathsOf(ITransformOperationSpec spec) =>
        spec is ITransformBranchingSpec branching ? branching.Paths : [];

    /// <inheritdoc />
    public void Attach(PipelineTransformConfiguration stage, string pathName, List<PipelineTransformConfiguration> stages)
    {
        var path = stage.Paths.First(candidate => string.Equals(candidate.Name, pathName, StringComparison.Ordinal));
        path.Stages = stages;
    }

    private static void PassOrReject(
        BranchPaths branch,
        PipelineTransformConfiguration config,
        List<IDictionary<string, object?>> unmatched,
        ITransformContext context,
        List<IDictionary<string, object?>> output)
    {
        if (!branch.Unmatched.RejectsRecord)
        {
            output.AddRange(unmatched);
            return;
        }

        foreach (var record in unmatched)
        {
            context.Reject(config.Name, string.Concat("Record rejected by branch '", config.Name, "': it meets no path"), record);
        }
    }

    private static string? FirstProblem(ITransformBranchingSpec branching)
    {
        if (string.IsNullOrWhiteSpace(branching.Unmatched))
        {
            return "a branch states what a record that matches no path causes (unmatched)";
        }

        if (ReferenceEquals(UnmatchedActions.ByName(branching.Unmatched), UnmatchedActions.NotFound))
        {
            return $"unmatched '{branching.Unmatched}' is not an unmatched action";
        }

        if (branching.Paths.Count == 0)
        {
            return "a branch names at least one path";
        }

        return PathProblem(branching.Paths);
    }

    private static string? PathProblem(IReadOnlyList<ITransformPathSpec> paths)
    {
        if (paths.Any(path => string.IsNullOrWhiteSpace(path.Name)))
        {
            return "every path is named";
        }

        var dotted = paths.FirstOrDefault(path => path.Name.Contains('.', StringComparison.Ordinal));
        if (dotted is not null)
        {
            return $"the path name '{dotted.Name}' cannot contain '.'";
        }

        var twice = paths.GroupBy(path => path.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (twice is not null)
        {
            return $"path '{twice.Key}' is listed twice";
        }

        var blank = paths.FirstOrDefault(path => string.IsNullOrWhiteSpace(path.Condition));
        return blank is null ? null : $"path '{blank.Name}' has no condition";
    }
}
