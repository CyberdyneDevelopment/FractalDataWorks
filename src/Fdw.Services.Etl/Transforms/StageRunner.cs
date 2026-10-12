using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Orders a list of stages and runs it, and answers what a list needs of the run: the one place that knows how a stage list
/// is read, for the stages of a pipeline and for the stages inside a Branch path alike.
/// </summary>
/// <remarks>
/// FDW-1120. Both pipelines order their stages here and ask here whether any stage, however deeply nested, needs the whole
/// input; a Branch runs the stages of a path through <see cref="RunBatch"/>. The policy for a failing stage (continue on
/// error, maximum errors) stays with the pipeline that owns the run: a failure inside a path fails the Branch stage, and the
/// pipeline applies its policy to that stage as it does to any other.
/// </remarks>
internal static class StageRunner
{
    /// <summary>Returns the enabled stages of a list in execution order.</summary>
    /// <param name="stages">The stages, in any order; null is an empty list.</param>
    /// <returns>The enabled stages sorted by <see cref="PipelineTransformConfiguration.ExecutionOrder"/>.</returns>
    public static List<PipelineTransformConfiguration> Ordered(IEnumerable<PipelineTransformConfiguration>? stages) =>
        stages is null ? [] : stages.Where(stage => stage.IsEnabled).OrderBy(stage => stage.ExecutionOrder).ToList();

    /// <summary>Returns whether any enabled stage in the list, or inside one of its paths, needs the whole input at once.</summary>
    /// <param name="stages">The stages to look through.</param>
    /// <returns>True when a run over this list cannot stream.</returns>
    public static bool RequiresCompleteInput(IEnumerable<PipelineTransformConfiguration>? stages)
    {
        foreach (var stage in Ordered(stages))
        {
            if (OptionTransformTypes.ByName(stage.OperationType).RequiresCompleteInput)
            {
                return true;
            }

            if (stage.Paths.Any(path => RequiresCompleteInput(path.Stages)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Runs a list of stages over a batch of records, each stage on what the one before produced.</summary>
    /// <param name="stages">The stages to run; they are ordered here.</param>
    /// <param name="records">The records the first stage reads.</param>
    /// <param name="context">The run's transform context.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The records the last stage produced, or the failure of the first stage that failed.</returns>
    public static async Task<IGenericResult<List<IDictionary<string, object?>>>> RunBatch(
        IEnumerable<PipelineTransformConfiguration> stages,
        IEnumerable<IDictionary<string, object?>> records,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        var current = records.ToList();
        foreach (var stage in Ordered(stages))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var transformType = OptionTransformTypes.ByName(stage.OperationType);
            if (ReferenceEquals(transformType, OptionTransformTypes.NotFound))
            {
                return GenericResult<List<IDictionary<string, object?>>>.Failure(EtlLog.UnknownTransformType(context.Logger, stage.OperationType));
            }

            var produced = await transformType.TransformBatch(current, stage, context, cancellationToken).ConfigureAwait(false);
            if (!produced.IsSuccess)
            {
                return produced.ToNewResult<List<IDictionary<string, object?>>>();
            }

            current = produced.Value?.ToList() ?? [];
        }

        return GenericResult<List<IDictionary<string, object?>>>.Success(current);
    }
}
