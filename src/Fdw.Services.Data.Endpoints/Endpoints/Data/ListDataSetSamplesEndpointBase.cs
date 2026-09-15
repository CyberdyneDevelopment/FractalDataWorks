using System;
using System.Collections.Generic;
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

/// <summary>Lists a data set's hand-picked samples.</summary>
public abstract class ListDataSetSamplesEndpointBase : Endpoint<DataSetNameRequest, List<DataSetSampleResponse>>
{
    private readonly DataSetConfigurationProvider _dataSets;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly ILogger<ListDataSetSamplesEndpointBase> _logger;

    /// <inheritdoc />
    protected ListDataSetSamplesEndpointBase(
        DataSetConfigurationProvider dataSets,
        IDataGatewayProvider dataGateways,
        ILogger<ListDataSetSamplesEndpointBase>? logger = null)
    {
        _dataSets = dataSets;
        _dataGateways = dataGateways;
        _logger = logger ?? NullLogger<ListDataSetSamplesEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/datasets/{Name}/samples");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:read");
#endif
        Summary(s =>
        {
            s.Summary = "List a data set's samples";
            s.Description = "Returns the data set's hand-picked sample sets, each with its own rows.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DataSetNameRequest req, CancellationToken ct)
    {
        DataSetSampleEndpointLog.ListingSamples(_logger, req.Name);

        var dataSet = await _dataSets.Get(req.Name, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) { await SendError(dataSet, ct).ConfigureAwait(false); return; }
        if (dataSet.Value is null) { await Send.NotFoundAsync(ct).ConfigureAwait(false); return; }

        var fieldNamesById = dataSet.Value.Fields.ToDictionary(f => f.Id, f => f.Name);

        var samples = await DataSetSampleReader.ListForDataSet(
            _dataGateways.ByName("Main"), _dataSets.DataStoreName, _dataSets.PathName, dataSet.Value.Id, fieldNamesById, ct)
            .ConfigureAwait(false);
        if (samples.IsFailure)
        {
            DataSetSampleEndpointLog.SamplesListFailed(_logger, req.Name);
            await SendError(samples, ct).ConfigureAwait(false);
            return;
        }

        DataSetSampleEndpointLog.SamplesListed(_logger, req.Name, samples.Value!.Count);
        await Send.OkAsync(samples.Value!, ct).ConfigureAwait(false);
    }

    private Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(errorResponse, ct);
    }
}
