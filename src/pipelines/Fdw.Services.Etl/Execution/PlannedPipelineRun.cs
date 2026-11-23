using System.Collections.Generic;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// A pipeline run that has been judged and is ready to be opened (FDW-6141): the pipeline, its resolved parameter values, the operational store its row is written to,
/// the parameters the pipeline declares and the origin its row states.
/// </summary>
/// <param name="Pipeline">The pipeline.</param>
/// <param name="Values">The parameter values, as the types the pipeline declares.</param>
/// <param name="Store">The operational store the run row is written to.</param>
/// <param name="Declared">The parameters the pipeline declares, or null when it declares none.</param>
/// <param name="Origin">What the run row states about where the run came from.</param>
internal sealed record PlannedPipelineRun(
    IPipelineImplementationConfiguration Pipeline, IReadOnlyDictionary<string, object?> Values, string Store, IReadOnlyList<IPipelineRunParameter>? Declared, PipelineRunOrigin Origin);
