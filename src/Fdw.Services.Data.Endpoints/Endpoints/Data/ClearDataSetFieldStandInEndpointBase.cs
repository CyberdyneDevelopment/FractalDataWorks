using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Clears a field's stand-in strategy (FDW-982): <c>DELETE /datasets/{Name}/fields/{Field}/standin</c>.</summary>
/// <remarks>
/// A soft delete: the current stand-in row is retired and marked deleted, and the field then reads as
/// having none, which is the 404 <see cref="GetDataSetFieldStandInEndpointBase"/> already answers for an
/// unset field. Answers 204 when a stand-in was cleared. A field that does not exist, or has no stand-in,
/// answers 404: there is nothing to clear, and the caller is told so rather than handed a success.
/// </remarks>
public abstract class ClearDataSetFieldStandInEndpointBase : Endpoint<DataSetFieldRequest>
{
    private readonly DataSetConfigurationProvider _dataSets;
    private readonly IConfigurationGatewayProvider _configurationGateways;
    private readonly ILogger<ClearDataSetFieldStandInEndpointBase> _logger;

    /// <inheritdoc />
    protected ClearDataSetFieldStandInEndpointBase(
        DataSetConfigurationProvider dataSets,
        IConfigurationGatewayProvider configurationGateways,
        ILogger<ClearDataSetFieldStandInEndpointBase>? logger = null)
    {
        _dataSets = dataSets;
        _configurationGateways = configurationGateways;
        _logger = logger ?? NullLogger<ClearDataSetFieldStandInEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/datasets/{Name}/fields/{Field}/standin");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:write");
#endif
        Summary(s =>
        {
            s.Summary = "Clear a field's stand-in strategy";
            s.Description = "Removes the stand-in strategy set on a data set field. 204 when one was cleared; 404 when the field has none.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DataSetFieldRequest req, CancellationToken ct)
    {
        StandInEndpointLog.ClearingStandIn(_logger, req.Name, req.Field);

        var resolved = await DataSetFieldResolver.Resolve(_dataSets, req.Name, req.Field, ct).ConfigureAwait(false);
        if (resolved.IsFailure) { await SendError(resolved, ct).ConfigureAwait(false); return; }
        if (resolved.Value is not { } found)
        {
            StandInEndpointLog.FieldNotFound(_logger, req.Name, req.Field);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var gateway = _configurationGateways.Get(_dataSets.DataStoreName);
        if (gateway.IsFailure) { await SendError(gateway, ct).ConfigureAwait(false); return; }

        var cleared = await DataSetFieldStandInWriter.Clear(
            gateway.Value!, _dataSets.DataStoreName, _dataSets.PathName, found.Field.Id, ct)
            .ConfigureAwait(false);
        if (cleared.IsFailure)
        {
            StandInEndpointLog.StandInWriteFailed(_logger, req.Name, req.Field);
            await SendError(cleared, ct).ConfigureAwait(false);
            return;
        }

        if (!cleared.Value)
        {
            StandInEndpointLog.NoStandInToClear(_logger, req.Name, req.Field);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        StandInEndpointLog.StandInCleared(_logger, req.Name, req.Field);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    private async Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}
