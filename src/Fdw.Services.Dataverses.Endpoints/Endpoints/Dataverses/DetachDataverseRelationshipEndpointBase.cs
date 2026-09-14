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

/// <summary>Removes a declared relationship from a dataverse — removes one edge from its map.</summary>
/// <remarks>Soft-deletes the one row by its own id, not by re-saving the aggregate — see
/// <see cref="CreateDataverseRelationshipEndpointBase"/> for why.</remarks>
public abstract class DetachDataverseRelationshipEndpointBase : Endpoint<DetachDataverseRelationshipRequest>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataverseAccessPolicy _access;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly ILogger<DetachDataverseRelationshipEndpointBase> _logger;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <inheritdoc />
    protected DetachDataverseRelationshipEndpointBase(
        DataverseConfigurationProvider dataverses,
        IDataverseAccessPolicy access,
        IDataGatewayProvider dataGateways,
        ILogger<DetachDataverseRelationshipEndpointBase>? logger = null)
    {
        _dataverses = dataverses;
        _access = access;
        _dataGateways = dataGateways;
        _logger = logger ?? NullLogger<DetachDataverseRelationshipEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/dataverses/{Name}/relationships/{RelationshipId}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("dataverses:write");
#endif
        Summary(s =>
        {
            s.Summary = "Remove a relationship from a dataverse";
            s.Description = "Removes one edge from the dataverse's map.";
        });
        Tags("Dataverses");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DetachDataverseRelationshipRequest req, CancellationToken ct)
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

        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseRelationship");
        var existingCommand = new QueryCommand<DataverseRelationshipConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = req.RelationshipId },
                        new FilterCondition { PropertyName = "DataverseImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataverse.Value.Id },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existing = await Gateway.Execute<IEnumerable<DataverseRelationshipConfiguration>>(existingCommand, target, ct).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            await SendFailure(500, existing.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        var found = new List<DataverseRelationshipConfiguration>(existing.Value ?? []);
        if (found.Count == 0)
        {
            var notFound = GenericResult.Failure(
                DataversesResultCodes.ByName("DataverseChildNotFound"), _logger,
                ResultDetails.Create("name", req.Name, "kind", "relationship", "id", req.RelationshipId.ToString()));
            await SendFailure(404, notFound.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        var row = found[0];
        row.IsDeleted = true;
        row.IsCurrent = false;
        // Same reason as the attach side's CreateDate stamp: the translator writes every mapped
        // column explicitly, so ModifyDate has to be set here or it inserts as 0001-01-01. (FDW-793)
        row.ModifyDate = DateTimeOffset.UtcNow;

        var updateCommand = new UpdateCommand<DataverseRelationshipConfiguration>(row)
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
            Title = "Relationship was not removed",
            Detail = reason,
            Instance = HttpContext.Request.Path.HasValue ? HttpContext.Request.Path.Value : null,
        }, ct);
    }
}
