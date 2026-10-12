using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The load definition a saved pipeline would carry after an update is applied (FDW-1105): what the update body states,
/// and the stored definition where it states nothing, in the shape a body carries so the one validator that judges bodies
/// can judge it.
/// </summary>
/// <remarks>
/// Built by <see cref="PipelineLoadPatch.Effective"/>; nothing else makes one, so the rule for which parts a body replaces
/// and which it leaves alone has one home.
/// </remarks>
public sealed class EffectivePipelineLoad : IPipelineLoadFields
{
    /// <inheritdoc />
    public string? LoadMode { get; init; }

    /// <inheritdoc />
    public PipelineLoadOptions? LoadOptions { get; init; }

    /// <inheritdoc />
    public IList<PipelineLoadStep>? LoadSteps { get; init; }

    /// <inheritdoc />
    public IList<PipelineLoadStep>? RejectSteps { get; init; }

    /// <inheritdoc />
    public IList<PipelineLoadColumnSource>? ValueSources { get; init; }

    /// <inheritdoc />
    public IList<PipelineParameterDeclaration>? Parameters { get; init; }
}
