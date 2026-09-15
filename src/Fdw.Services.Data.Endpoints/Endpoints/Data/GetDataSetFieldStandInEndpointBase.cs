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

/// <summary>Reads a field's currently active stand-in, if any.</summary>
/// <remarks>
/// Dedicated read rather than folded into GetDataSetFieldsEndpointBase: the console cannot infer
/// "which strategy table has a row" from the field payload alone, so this is the one place that
/// says it, per react-ui-design's own console ask.
/// </remarks>
public abstract class GetDataSetFieldStandInEndpointBase : Endpoint<DataSetFieldRequest, DataSetFieldStandInResponse>
{
    private readonly DataSetConfigurationProvider _dataSets;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly ILogger<GetDataSetFieldStandInEndpointBase> _logger;

    /// <inheritdoc />
    protected GetDataSetFieldStandInEndpointBase(
        DataSetConfigurationProvider dataSets,
        IDataGatewayProvider dataGateways,
        ILogger<GetDataSetFieldStandInEndpointBase>? logger = null)
    {
        _dataSets = dataSets;
        _dataGateways = dataGateways;
        _logger = logger ?? NullLogger<GetDataSetFieldStandInEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/datasets/{Name}/fields/{Field}/standin");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get a field's stand-in strategy";
            s.Description = "Returns the stand-in strategy currently active on a data set field, or 404 if none is set.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DataSetFieldRequest req, CancellationToken ct)
    {
        StandInEndpointLog.GettingStandIn(_logger, req.Name, req.Field);

        var resolved = await DataSetFieldResolver.Resolve(_dataSets, req.Name, req.Field, ct).ConfigureAwait(false);
        if (resolved.IsFailure) { await SendError(resolved, ct).ConfigureAwait(false); return; }
        if (resolved.Value is not { } found)
        {
            StandInEndpointLog.FieldNotFound(_logger, req.Name, req.Field);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var active = await DataSetFieldStandInReader.FindActive(
            _dataGateways.ByName("Main"), _dataSets.DataStoreName, _dataSets.PathName, found.Field.Id, found.Field.Name, ct)
            .ConfigureAwait(false);
        if (active.IsFailure)
        {
            StandInEndpointLog.StandInReadFailed(_logger, req.Name, req.Field);
            await SendError(active, ct).ConfigureAwait(false);
            return;
        }

        if (active.Value is null) { await Send.NotFoundAsync(ct).ConfigureAwait(false); return; }

        await Send.OkAsync(active.Value, ct).ConfigureAwait(false);
    }

    private Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(errorResponse, ct);
    }
}
