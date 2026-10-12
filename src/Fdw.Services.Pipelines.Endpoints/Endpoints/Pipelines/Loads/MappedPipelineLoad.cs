using System.Collections.Generic;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// A pipeline body's load definition as the engine's configuration rows (FDW-1110): the load mode, the load rows (the
/// pipeline's own load or its steps) and the declared run parameters. Null means the body stated none, the way a
/// pipeline's transforms are null when it has none.
/// </summary>
/// <param name="LoadMode">The load mode name, or <see langword="null"/> when the pipeline states load steps instead.</param>
/// <param name="LoadSteps">The load rows, or <see langword="null"/> when there are none.</param>
/// <param name="RunParameters">The declared run parameters, or <see langword="null"/> when there are none.</param>
public sealed record MappedPipelineLoad(
    string? LoadMode,
    IList<PipelineLoadStepConfiguration>? LoadSteps,
    IList<PipelineRunParameterConfiguration>? RunParameters);
