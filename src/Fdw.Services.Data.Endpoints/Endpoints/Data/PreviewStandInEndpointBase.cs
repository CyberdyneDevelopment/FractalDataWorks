using System.Collections.Generic;
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

/// <summary>Generates example values for a stand-in strategy without persisting it.</summary>
public abstract class PreviewStandInEndpointBase : Endpoint<PreviewStandInRequest, PreviewStandInResponse>
{
    private readonly DataSetConfigurationProvider _dataSets;
    private readonly IConfigurationGatewayProvider _configurationGateways;
    private readonly ILogger<PreviewStandInEndpointBase> _logger;

    /// <inheritdoc />
    protected PreviewStandInEndpointBase(
        DataSetConfigurationProvider dataSets,
        IConfigurationGatewayProvider configurationGateways,
        ILogger<PreviewStandInEndpointBase>? logger = null)
    {
        _dataSets = dataSets;
        _configurationGateways = configurationGateways;
        _logger = logger ?? NullLogger<PreviewStandInEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datasets/{Name}/fields/{Field}/standin/preview");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:read");
#endif
        Summary(s =>
        {
            s.Summary = "Preview a stand-in strategy";
            s.Description = "Generates example values for a stand-in strategy without saving it.";
        });
        Tags("DataSets");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(PreviewStandInRequest req, CancellationToken ct)
    {
        var count = req.Count is > 0 and <= 100 ? req.Count : 5;
        StandInEndpointLog.PreviewingStandIn(_logger, req.Name, req.Field, req.Strategy, count);

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
        if (resolved.Value is null)
        {
            StandInEndpointLog.FieldNotFound(_logger, req.Name, req.Field);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        IReadOnlyList<string>? candidates = null;
        if (req.Strategy == "DrawFromDataSet" && req.SourceFieldId is { } sourceFieldId)
        {
            var gateway = _configurationGateways.Get(_dataSets.DataStoreName);
            if (gateway.IsFailure) { await SendError(gateway, ct).ConfigureAwait(false); return; }

            var drawn = await DataSetSampleReader.DistinctValuesForField(
                gateway.Value!, _dataSets.DataStoreName, _dataSets.PathName, sourceFieldId, ct).ConfigureAwait(false);
            if (drawn.IsFailure) { await SendError(drawn, ct).ConfigureAwait(false); return; }
            candidates = drawn.Value;
        }

        var values = StandInValueGenerator.Generate(req, count, candidates);
        await Send.OkAsync(new PreviewStandInResponse { Strategy = req.Strategy, Values = values }, ct).ConfigureAwait(false);
    }

    private async Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}
