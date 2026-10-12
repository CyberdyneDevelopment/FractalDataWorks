using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Replaces one named sample's rows for a data set.</summary>
public abstract class UpsertDataSetSampleEndpointBase : Endpoint<DataSetSampleRequest, DataSetSampleResponse>
{
    private readonly DataSetConfigurationProvider _dataSets;
    private readonly IConfigurationGatewayProvider _configurationGateways;
    private readonly ILogger<UpsertDataSetSampleEndpointBase> _logger;

    /// <inheritdoc />
    /// <remarks>
    /// The rows are configuration rows in the data set store the host declared, so they go through that store's
    /// configuration gateway: the main gateway refuses a request over a host-declared store.
    /// </remarks>
    protected UpsertDataSetSampleEndpointBase(
        DataSetConfigurationProvider dataSets,
        IConfigurationGatewayProvider configurationGateways,
        ILogger<UpsertDataSetSampleEndpointBase>? logger = null)
    {
        _dataSets = dataSets;
        _configurationGateways = configurationGateways;
        _logger = logger ?? NullLogger<UpsertDataSetSampleEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datasets/{Name}/samples");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:write");
#endif
        Summary(s =>
        {
            s.Summary = "Replace a data set sample";
            s.Description = "Replaces the named sample's rows in full, creating it if it does not exist yet.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DataSetSampleRequest req, CancellationToken ct)
    {
        DataSetSampleEndpointLog.UpsertingSample(_logger, req.Name, req.SampleName, req.Rows.Count);

        var dataSet = await _dataSets.Get(req.Name, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) { await SendError(dataSet, ct).ConfigureAwait(false); return; }
        if (dataSet.Value is null) { await Send.NotFoundAsync(ct).ConfigureAwait(false); return; }

        var fieldIdsByName = dataSet.Value.Fields.ToDictionary(f => f.Name, f => f.Id, System.StringComparer.OrdinalIgnoreCase);

        var gateway = _configurationGateways.Get(_dataSets.DataStoreName);
        if (gateway.IsFailure) { await SendError(gateway, ct).ConfigureAwait(false); return; }

        var upserted = await DataSetSampleWriter.Upsert(
            gateway.Value!, _dataSets.DataStoreName, _dataSets.PathName, dataSet.Value.Id, fieldIdsByName, req, _logger, ct)
            .ConfigureAwait(false);
        if (upserted.IsFailure)
        {
            DataSetSampleEndpointLog.SampleWriteFailed(_logger, req.Name, req.SampleName);
            await SendError(upserted, ct).ConfigureAwait(false);
            return;
        }

        DataSetSampleEndpointLog.SampleUpserted(_logger, req.Name, req.SampleName, req.Rows.Count);
        await Send.OkAsync(upserted.Value!, ct).ConfigureAwait(false);
    }

    private async Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}
