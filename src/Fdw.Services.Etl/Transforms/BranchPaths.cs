using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Predicates;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// The paths of one Branch stage, ready to route records: its unmatched action and each path with its condition read.
/// </summary>
internal sealed class BranchPaths
{
    private const string Kind = "Branch";

    private BranchPaths(IUnmatchedAction unmatched, List<(PipelineTransformPathConfiguration Path, IRecordPredicate Condition)> paths)
    {
        Unmatched = unmatched;
        Paths = paths;
    }

    /// <summary>Gets what happens to a record that matches no path.</summary>
    public IUnmatchedAction Unmatched { get; }

    /// <summary>Gets the paths in the order they are tried, each with its condition read.</summary>
    public List<(PipelineTransformPathConfiguration Path, IRecordPredicate Condition)> Paths { get; }

    /// <summary>Reads a Branch stage's unmatched action and path conditions for one run.</summary>
    /// <param name="config">The Branch stage.</param>
    /// <param name="context">The run's transform context; its parameters replace the <c>{{name}}</c> references.</param>
    /// <returns>The prepared paths, or the failure naming what the stage lacks or what cannot be read.</returns>
    public static IGenericResult<BranchPaths> Prepare(PipelineTransformConfiguration config, ITransformContext context)
    {
        if (string.IsNullOrWhiteSpace(config.Unmatched))
        {
            return GenericResult<BranchPaths>.Failure(TransformLog.TransformParamsInvalid(
                context.Logger, config.Name, Kind, "a branch states what a record that matches no path causes (unmatched)"));
        }

        var unmatched = UnmatchedActions.ByName(config.Unmatched);
        if (ReferenceEquals(unmatched, UnmatchedActions.NotFound))
        {
            return GenericResult<BranchPaths>.Failure(TransformLog.TransformParamsInvalid(
                context.Logger, config.Name, Kind, $"unmatched '{config.Unmatched}' is not an unmatched action"));
        }

        if (config.Paths.Count == 0)
        {
            return GenericResult<BranchPaths>.Failure(TransformLog.TransformParamsInvalid(
                context.Logger, config.Name, Kind, "a branch has at least one path"));
        }

        var paths = new List<(PipelineTransformPathConfiguration Path, IRecordPredicate Condition)>();
        foreach (var path in config.Paths.OrderBy(candidate => candidate.Ordinal))
        {
            var condition = Read(config, path, context);
            if (condition.IsFailure)
            {
                return condition.ToNewResult<BranchPaths>();
            }

            paths.Add((path, condition.Value!));
        }

        return GenericResult<BranchPaths>.Success(new BranchPaths(unmatched, paths));
    }

    /// <summary>Returns the index of the first path whose condition the record meets, or -1 when it meets none.</summary>
    /// <param name="record">The record to route.</param>
    /// <returns>The path's position in <see cref="Paths"/>, or -1.</returns>
    public int FirstMatch(IDictionary<string, object?> record)
    {
        for (var index = 0; index < Paths.Count; index++)
        {
            if (Paths[index].Condition.Evaluate(record))
            {
                return index;
            }
        }

        return -1;
    }

    private static IGenericResult<IRecordPredicate> Read(PipelineTransformConfiguration config, PipelineTransformPathConfiguration path, ITransformContext context)
    {
        var expression = PipelineRunParameterReferences.Resolve(
            string.Concat("transform '", config.Name, "' path '", path.Name, "'"), path.Condition, context.Parameters, context.Logger);
        return expression.IsFailure
            ? expression.ToNewResult<IRecordPredicate>()
            : RecordPredicates.Parse(expression.Value!, context.Logger);
    }
}
