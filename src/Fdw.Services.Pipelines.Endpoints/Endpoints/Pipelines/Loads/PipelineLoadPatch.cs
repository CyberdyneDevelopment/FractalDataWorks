using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// What an update body changes about a saved pipeline's load definition and what it leaves alone, held in one place so
/// saving a pipeline and checking an edit of it (FDW-1105) cannot read the same body two ways.
/// </summary>
/// <remarks>
/// Contract v3.1 B: <c>loadMode</c>, <c>loadOptions</c>, <c>loadSteps</c> and <c>valueSources</c> are ONE UNIT. A body that
/// states none of the four leaves the stored unit alone; a body that states any replaces it whole with what the body says
/// (what it leaves out is cleared). The run parameters are their own: stated replaces, absent keeps. So are the reject
/// steps (FDW-1142): stated replaces, an empty list removes them, absent keeps; they are never part of the unit.
/// </remarks>
public static class PipelineLoadPatch
{
    /// <summary>Gets whether the body states any of the four properties that make up the load unit.</summary>
    /// <param name="body">The update body.</param>
    /// <returns><see langword="true"/> when the body replaces the stored load unit.</returns>
    public static bool UnitStated(IPipelineLoadFields body) =>
        body.LoadMode is not null || body.LoadOptions is not null || body.LoadSteps is not null || body.ValueSources is not null;

    /// <summary>Gets whether the body changes anything about the load definition: the unit, the reject steps or the run parameters.</summary>
    /// <param name="body">The update body.</param>
    /// <returns><see langword="true"/> when the body states the load unit, the reject steps or the run parameters.</returns>
    public static bool Changes(IPipelineLoadFields body) => UnitStated(body) || body.RejectSteps is not null || body.Parameters is not null;

    /// <summary>
    /// The load rows the saved pipeline would carry once the body is applied: the stated or stored rows that load the output,
    /// the stated or stored reject steps, and the stated or stored run parameters, each part replaced on its own.
    /// </summary>
    /// <param name="request">The update body.</param>
    /// <param name="stated">The body's load definition, mapped.</param>
    /// <param name="stored">The saved pipeline's load definition, as <see cref="Stored"/> reads it.</param>
    /// <returns>The definition to judge and save.</returns>
    public static MappedPipelineLoad Apply(IPipelineLoadFields request, MappedPipelineLoad stated, MappedPipelineLoad stored)
    {
        var unit = UnitStated(request);
        List<PipelineLoadStepConfiguration> rows =
        [
            .. LoadPhaseRows.Loading(unit ? stated.LoadSteps : stored.LoadSteps),
            .. LoadPhaseRows.Rejecting(request.RejectSteps is not null ? stated.LoadSteps : stored.LoadSteps),
        ];
        return new MappedPipelineLoad(
            unit ? stated.LoadMode : stored.LoadMode,
            rows.Count > 0 ? rows : null,
            request.Parameters is not null ? stated.RunParameters : stored.RunParameters);
    }

    /// <summary>The stored load definition as the engine's configuration rows.</summary>
    /// <param name="original">The saved pipeline.</param>
    /// <returns>The stored definition; every part is <see langword="null"/> for a pipeline that carries none.</returns>
    public static MappedPipelineLoad Stored(IPipelineImplementationConfiguration original) =>
        original is IPipelineLoadConfiguration load
            ? new MappedPipelineLoad(load.LoadMode, load.LoadSteps, load.RunParameters)
            : new MappedPipelineLoad(null, null, null);

    /// <summary>The load definition the saved pipeline would carry once the body is applied, in the shape a body carries.</summary>
    /// <param name="request">The update body.</param>
    /// <param name="original">The saved pipeline.</param>
    /// <returns>The body's own parts where it states them, the stored parts read back where it states none.</returns>
    public static IPipelineLoadFields Effective(UpdatePipelineRequest request, IPipelineImplementationConfiguration original)
    {
        var stored = original as IPipelineLoadConfiguration;
        var unit = UnitStated(request);
        return new EffectivePipelineLoad
        {
            LoadMode = unit ? request.LoadMode : stored?.LoadMode,
            LoadOptions = unit ? request.LoadOptions : stored is null ? null : PipelineLoadReader.Options(stored),
            LoadSteps = unit ? request.LoadSteps : stored is null ? null : PipelineLoadReader.Steps(stored),
            RejectSteps = request.RejectSteps is not null ? request.RejectSteps : stored is null ? null : PipelineLoadReader.RejectSteps(stored),
            ValueSources = unit ? request.ValueSources : stored is null ? null : PipelineLoadReader.ValueSources(stored),
            Parameters = request.Parameters is not null ? request.Parameters : stored is null ? null : PipelineLoadReader.Parameters(stored),
        };
    }

    /// <summary>
    /// The destination data set the saved pipeline will have: a side the request names replaces the whole binding, as the
    /// concrete endpoint's update writes it.
    /// </summary>
    /// <param name="request">The update body.</param>
    /// <param name="existing">The saved pipeline as the detail response reads it.</param>
    /// <returns>The destination data set name, or <see langword="null"/> when the saved pipeline will name none.</returns>
    public static string? EffectiveDestinationDataSet(UpdatePipelineRequest request, PipelineDetailResponse existing)
    {
        if (request.DestinationDataSet is not null)
        {
            return request.DestinationDataSet;
        }

        return request.DestinationDataStoreName is not null ? null : existing.DestinationDataSet;
    }

    /// <summary>
    /// The filter expressions of the transforms the saved pipeline will have: the request's when it states transforms,
    /// otherwise the stored ones, each with the stage it sits on.
    /// </summary>
    /// <param name="request">The update body.</param>
    /// <param name="original">The saved pipeline.</param>
    /// <returns>Each transform's name and filter expression.</returns>
    public static IReadOnlyList<(string? Stage, string? Expression)> Expressions(UpdatePipelineRequest request, IPipelineImplementationConfiguration original)
    {
        if (request.Transforms is not null)
        {
            return PipelineDraft.StageFilterExpressions(request.Transforms);
        }

        return original is IEtlPipelineImplementationConfiguration { Transforms: { } stored }
            ? [.. stored.Select(t => ((string?)t.Name, t.FilterExpression))]
            : [];
    }
}
