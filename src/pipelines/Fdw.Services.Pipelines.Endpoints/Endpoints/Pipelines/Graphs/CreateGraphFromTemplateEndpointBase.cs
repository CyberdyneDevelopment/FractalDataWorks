using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Makes a pipeline from a template (FDW-3379): the template, the new pipeline's name and the values for the template's parameters.
/// </summary>
/// <remarks>
/// The new pipeline is an ordinary, separate pipeline. It is made by <see cref="TemplateFill"/> and then read, judged and saved exactly as a graph typed in the builder is,
/// so every rule of a save holds for it, including the permission to write scripts when the template holds one.
/// </remarks>
public abstract class CreateGraphFromTemplateEndpointBase : CrudCreateEndpointBase<GraphFromTemplatePayload, GraphPayload>
{
    private readonly PipelineServiceConfigurationProvider _provider;
    private readonly StageChainLookup _chains;
    private readonly IDataSetConfigurationProvider _dataSets;

    /// <summary>Initializes a new instance of the <see cref="CreateGraphFromTemplateEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="transformations">Resolves the named transformations a field mapping's chain applies.</param>
    /// <param name="dataSets">The data sets, read for the type of the columns a destination's value sources fill.</param>
    protected CreateGraphFromTemplateEndpointBase(
        ILogger<CreateGraphFromTemplateEndpointBase> logger, PipelineServiceConfigurationProvider provider, ITransformationConfigurationProvider transformations,
        IDataSetConfigurationProvider dataSets) : base(logger)
    {
        _provider = provider;
        _chains = new StageChainLookup(transformations, logger);
        _dataSets = dataSets;
    }

    /// <inheritdoc />
    protected override string ResourceName => "pipelines";

    /// <inheritdoc />
    protected override string Route => "/pipelines/from-template";

    /// <inheritdoc />
    protected override string EndpointSummary => "Make a pipeline from a template";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Makes a new pipeline from a template: each parameter given a value is filled in wherever its {{Name}} appears and is no longer a parameter; the others stay run parameters. The new pipeline is separate from the template and records which template and version it came from.";

    /// <inheritdoc />
    protected override string GetResourceName(GraphFromTemplatePayload request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(GraphFromTemplatePayload request, CancellationToken ct) =>
        GenericResult<bool>.Success(await PipelineDraft.NameTaken(_provider, request.Name, ct).ConfigureAwait(false));

    /// <inheritdoc />
    protected override async Task<IGenericResult<GraphPayload>> Create(GraphFromTemplatePayload request, CancellationToken ct)
    {
        var read = await _provider.Get(request.Template, ct).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<GraphPayload>();
        }

        if (read.Value is not GraphPipelineConfiguration { IsTemplate: true } template)
        {
            return GraphRefusal.Of<GraphPayload>($"'{request.Template}' is not a template: it does not exist, is not a graph pipeline, or is not marked as a template");
        }

        var filled = TemplateFill.Apply(
            GraphPayloadWriter.Write(template), template.ModifyDate, request.Name, [.. request.Values.Select(value => new KeyValuePair<string, string>(value.Name, value.Value))], Logger);
        if (filled.IsFailure || filled.Value is null)
        {
            return filled;
        }

        var graph = GraphPayloadReader.Read(filled.Value, Guid.CreateVersion7(), await GraphChainLookup.Resolve(_chains, filled.Value, ct).ConfigureAwait(false));
        if (graph.IsFailure || graph.Value is null)
        {
            return graph.ToNewResult<GraphPayload>();
        }

        return await GraphSave.Save(_provider, graph.Value, _dataSets, GraphScriptPermission.For(HttpContext), Logger, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(GraphPayload detail, CancellationToken ct) => Send.ResponseAsync(detail, 201, ct);
}
