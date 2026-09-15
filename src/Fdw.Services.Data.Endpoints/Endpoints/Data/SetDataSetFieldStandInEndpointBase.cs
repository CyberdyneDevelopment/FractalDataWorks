using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Sets a field's stand-in strategy — a named action on a field, not a CRUD resource, so
/// this is a raw POST-shaped (PATCH) endpoint rather than a <c>CrudUpdateEndpointBase</c> derivative.</summary>
/// <remarks>
/// Mints a fresh <c>DataSetFieldStandIn</c> row on every call; the unique index on
/// (DataSetFieldId WHERE IsCurrent) retires whatever strategy was set before, the same
/// version-on-write shape <see cref="DataSetFieldStandInWriter"/> documents.
/// </remarks>
public abstract class SetDataSetFieldStandInEndpointBase : Endpoint<SetDataSetFieldStandInRequest, DataSetFieldStandInResponse>
{
    private readonly DataSetConfigurationProvider _dataSets;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly ILogger<SetDataSetFieldStandInEndpointBase> _logger;

    /// <inheritdoc />
    protected SetDataSetFieldStandInEndpointBase(
        DataSetConfigurationProvider dataSets,
        IDataGatewayProvider dataGateways,
        ILogger<SetDataSetFieldStandInEndpointBase>? logger = null)
    {
        _dataSets = dataSets;
        _dataGateways = dataGateways;
        _logger = logger ?? NullLogger<SetDataSetFieldStandInEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Patch("/datasets/{Name}/fields/{Field}/standin");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:write");
#endif
        Summary(s =>
        {
            s.Summary = "Set a field's stand-in strategy";
            s.Description = "Sets or replaces which stand-in strategy a data set field uses to generate placeholder values.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(SetDataSetFieldStandInRequest req, CancellationToken ct)
    {
        StandInEndpointLog.SettingStandIn(_logger, req.Name, req.Field, req.Strategy);

        if (ReferenceEquals(StandInStrategies.ByName(req.Strategy), StandInStrategies.NotFound))
        {
            await SendError(GenericResult.Failure(
                DataSetsResultCodes.ByName("StandInStrategyInvalid"), _logger,
                ResultDetails.Create("name", $"{req.Name}.{req.Field}", "strategy", req.Strategy)), ct).ConfigureAwait(false);
            return;
        }

        var validated = StandInParameterValidator.Validate(req.Name, req.Field, req.Strategy, req);
        if (validated.IsFailure) { await SendError(validated, ct).ConfigureAwait(false); return; }

        var resolved = await DataSetFieldResolver.Resolve(_dataSets, req.Name, req.Field, ct).ConfigureAwait(false);
        if (resolved.IsFailure) { await SendError(resolved, ct).ConfigureAwait(false); return; }
        if (resolved.Value is not { } found)
        {
            StandInEndpointLog.FieldNotFound(_logger, req.Name, req.Field);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var written = await DataSetFieldStandInWriter.Write(
            _dataGateways.ByName("Main"), _dataSets.DataStoreName, _dataSets.PathName, found.Field.Id, found.Field.Name, req, ct)
            .ConfigureAwait(false);
        if (written.IsFailure)
        {
            StandInEndpointLog.StandInWriteFailed(_logger, req.Name, req.Field);
            await SendError(written, ct).ConfigureAwait(false);
            return;
        }

        StandInEndpointLog.StandInSet(_logger, req.Name, req.Field, req.Strategy);
        await Send.OkAsync(written.Value!, ct).ConfigureAwait(false);
    }

    private Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(errorResponse, ct);
    }
}
