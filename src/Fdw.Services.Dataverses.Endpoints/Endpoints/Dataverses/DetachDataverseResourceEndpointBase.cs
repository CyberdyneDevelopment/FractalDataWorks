using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Detaches a resource from a dataverse — removes one node from its map.</summary>
/// <remarks>Soft-deletes the one row by attachment id, not by re-saving the aggregate — see
/// <see cref="AttachDataverseResourceEndpointBase"/> for why.</remarks>
public abstract class DetachDataverseResourceEndpointBase : Endpoint<DetachDataverseResourceRequest>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataverseAccessPolicy _access;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly ILogger<DetachDataverseResourceEndpointBase> _logger;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <inheritdoc />
    protected DetachDataverseResourceEndpointBase(
        DataverseConfigurationProvider dataverses,
        IDataverseAccessPolicy access,
        IDataGatewayProvider dataGateways,
        ILogger<DetachDataverseResourceEndpointBase>? logger = null)
    {
        _dataverses = dataverses;
        _access = access;
        _dataGateways = dataGateways;
        _logger = logger ?? NullLogger<DetachDataverseResourceEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/dataverses/{Name}/resources/{ResourceAttachmentId}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("dataverses:write");
#endif
        Summary(s =>
        {
            s.Summary = "Detach a resource from a dataverse";
            s.Description = "Removes one node from the dataverse's map.";
        });
        Tags("Dataverses");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DetachDataverseResourceRequest req, CancellationToken ct)
    {
        var dataverse = await _dataverses.Get(req.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure)
        {
            await SendFailure(500, dataverse.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        if (dataverse.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var permitted = await _access.MayWrite(dataverse.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure)
        {
            await SendFailure(403, permitted.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseResource");
        var existingCommand = new QueryCommand<DataverseResourceConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = req.ResourceAttachmentId },
                        new FilterCondition { PropertyName = "DataverseImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataverse.Value.Id },
                        // Defensive: resources have no update path today, so only one non-deleted
                        // version can exist per Id, but the same find-by-Id-and-not-deleted shape
                        // elsewhere in this domain has proven unsafe the moment an update path exists.
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existing = await Gateway.Execute<IEnumerable<DataverseResourceConfiguration>>(existingCommand, target, ct).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            await SendFailure(500, existing.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        var found = new List<DataverseResourceConfiguration>(existing.Value ?? []);
        if (found.Count == 0)
        {
            var notFound = GenericResult.Failure(
                DataversesResultCodes.ByName("DataverseChildNotFound"), _logger,
                ResultDetails.Create("name", req.Name, "kind", "resource", "id", req.ResourceAttachmentId.ToString()));
            await SendFailure(404, notFound.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        var row = found[0];
        row.IsDeleted = true;
        row.IsCurrent = false;
        // Same reason as the attach side's CreateDate stamp: the translator writes every mapped
        // column explicitly, so ModifyDate has to be set here or it inserts as 0001-01-01. (FDW-793)
        row.ModifyDate = DateTimeOffset.UtcNow;

        var updateCommand = new UpdateCommand<DataverseResourceConfiguration>(row)
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = row.Id }
            }
        };

        var retired = await Gateway.Execute<int>(updateCommand, target, ct).ConfigureAwait(false);
        if (retired.IsFailure)
        {
            await SendFailure(500, retired.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    private Task SendFailure(int statusCode, string? reason, CancellationToken ct)
    {
        HttpContext.Response.StatusCode = statusCode;
        HttpContext.Response.ContentType = "application/json";
        return HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = "Resource was not detached",
            Detail = reason,
            Instance = HttpContext.Request.Path.HasValue ? HttpContext.Request.Path.Value : null,
        }, ct);
    }
}
