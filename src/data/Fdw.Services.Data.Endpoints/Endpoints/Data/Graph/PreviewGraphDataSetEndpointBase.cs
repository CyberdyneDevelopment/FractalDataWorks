using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Endpoint base to read the rows of a Graph data set draft that is not saved (FDW-5454): <c>POST /datasets/graph/preview</c>.
/// </summary>
/// <remarks>
/// <para>
/// The body is the one <c>POST /datasets/graph</c> takes, with the most rows each source gives and, optionally, a component and output to read at.
/// The draft is checked first by the check <c>POST /datasets/graph/validate</c> runs, and a draft with a problem answers its problems and reads nothing. A sound draft is
/// read through the caller's own gateway, so every source is read for the caller with that caller's access, source by source, and nothing is stored.
/// </para>
/// <para>
/// Reading is the data route's grant: <c>datasets:read</c>, as the row routes ask it.
/// </para>
/// </remarks>
public abstract class PreviewGraphDataSetEndpointBase : Endpoint<PreviewGraphDataSetRequest, GraphDataSetPreviewPayload>
{
    private readonly ILogger<PreviewGraphDataSetEndpointBase> _logger;
    private readonly GraphDataSetSources _sources;
    private readonly IDataGatewayProvider _gateways;
    private readonly IGraphDataSetPreview? _preview;

    /// <summary>Initializes a new instance of the <see cref="PreviewGraphDataSetEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSets">Reads the data sets the draft reads.</param>
    /// <param name="dataStores">Reads the data stores the draft reads or intends to read.</param>
    /// <param name="gateways">Supplies the caller's gateway the sources are read through.</param>
    /// <param name="preview">Reads the draft's rows (optional; a host that does not register it answers that it has no runner).</param>
    protected PreviewGraphDataSetEndpointBase(
        ILogger<PreviewGraphDataSetEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        IDataStoreConfigurationProvider dataStores,
        IDataGatewayProvider gateways,
        IGraphDataSetPreview? preview = null)
    {
        _logger = logger;
        _sources = new GraphDataSetSources(dataSets, dataStores);
        _gateways = gateways;
        _preview = preview;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datasets/graph/preview");
        DontThrowIfValidationFails();
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:read");
#endif
        Summary(s =>
        {
            s.Summary = "Preview the rows of a Graph data set draft";
            s.Description = "Takes the body of POST /datasets/graph, the most rows to read from each source (maxRowsPerSource; left out, the host's configured number applies) and, optionally, a component and output to read at. A draft with a problem answers its problems and reads nothing; a sound draft answers its rows, read for the caller. Nothing is saved.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(PreviewGraphDataSetRequest req, CancellationToken ct)
    {
        var checkedDraft = await GraphDataSetValidator.Draft(req, _sources, _logger, ct).ConfigureAwait(false);
        if (checkedDraft.IsFailure || checkedDraft.Value is null)
        {
            await SendFailure(checkedDraft).ConfigureAwait(false);
            return;
        }

        var payload = checkedDraft.Value.Payload;
        if (!payload.IsValid)
        {
            await Send.OkAsync(new GraphDataSetPreviewPayload { IsValid = false, Problems = payload.Problems }, ct).ConfigureAwait(false);
            return;
        }

        var rows = await Read(checkedDraft.Value.Draft, req, ct).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
        {
            await SendFailure(rows).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new GraphDataSetPreviewPayload { IsValid = true, Rows = [.. rows.Value] }, ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> Read(
        GraphDataSetConfiguration draft, PreviewGraphDataSetRequest req, CancellationToken ct) =>
        _preview is { } preview
            ? await preview.Preview(_gateways.ByName(DataGatewayNames.Main), draft, new GraphPreviewRequest(req.MaxRowsPerSource, req.Component, req.Output), ct).ConfigureAwait(false)
            : GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetRunnerMissing"), _logger, ResultDetails.Create("dataSet", draft.Name));

    private async Task SendFailure(IGenericResult failure)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(failure, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}
