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

/// <summary>Reads the active stand-in of every field of a data set in one call.</summary>
/// <remarks>
/// The same read as <see cref="GetDataSetFieldStandInEndpointBase"/>, asked of all fields at once through
/// <see cref="DataSetFieldStandInReader"/>, so the console does not make one request per field. A field with no
/// stand-in comes back with a null <see cref="DataSetFieldStandInEntry.StandIn"/> rather than as a 404.
/// </remarks>
public abstract class GetDataSetStandInsEndpointBase : Endpoint<DataSetNameRequest, List<DataSetFieldStandInEntry>>
{
    private readonly DataSetConfigurationProvider _dataSets;
    private readonly IConfigurationGatewayProvider _configurationGateways;
    private readonly ILogger<GetDataSetStandInsEndpointBase> _logger;

    /// <inheritdoc />
    protected GetDataSetStandInsEndpointBase(
        DataSetConfigurationProvider dataSets,
        IConfigurationGatewayProvider configurationGateways,
        ILogger<GetDataSetStandInsEndpointBase>? logger = null)
    {
        _dataSets = dataSets;
        _configurationGateways = configurationGateways;
        _logger = logger ?? NullLogger<GetDataSetStandInsEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/datasets/{Name}/standins");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get the stand-ins of every field of a data set";
            s.Description = "Returns every field of the data set, in field order, each with its active stand-in or null when it has none.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DataSetNameRequest req, CancellationToken ct)
    {
        StandInEndpointLog.GettingStandIns(_logger, req.Name);

        var fields = await DataSetFieldResolver.ResolveAll(_dataSets, req.Name, ct).ConfigureAwait(false);
        if (fields.IsFailure) { await SendError(fields, ct).ConfigureAwait(false); return; }
        if (fields.Value is null)
        {
            StandInEndpointLog.DataSetNotFoundForStandIns(_logger, req.Name);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var gateway = _configurationGateways.Get(_dataSets.DataStoreName);
        if (gateway.IsFailure) { await SendError(gateway, ct).ConfigureAwait(false); return; }

        var active = await DataSetFieldStandInReader.FindActiveMany(
            gateway.Value!, _dataSets.DataStoreName, _dataSets.PathName, fields.Value.Select(f => (FieldId: f.Id, f.Name)).ToList(), _logger, ct)
            .ConfigureAwait(false);
        if (active.IsFailure)
        {
            StandInEndpointLog.StandInReadFailed(_logger, req.Name, "*");
            await SendError(active, ct).ConfigureAwait(false);
            return;
        }

        var standIns = active.Value!;
        StandInEndpointLog.StandInsRead(_logger, req.Name, fields.Value.Count, standIns.Count);
        await Send.OkAsync(fields.Value.Select(f => new DataSetFieldStandInEntry
        {
            FieldId = f.Id,
            Field = f.Name,
            StandIn = standIns.TryGetValue(f.Id, out var standIn) ? standIn : null,
        }).ToList(), ct).ConfigureAwait(false);
    }

    private async Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}
