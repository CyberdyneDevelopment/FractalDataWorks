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

/// <summary>Removes a member from a dataverse.</summary>
/// <remarks>Soft-deletes the one row by its own id, not by re-saving the aggregate — see
/// <see cref="AddDataverseMemberEndpointBase"/> for why.</remarks>
public abstract class RemoveDataverseMemberEndpointBase : Endpoint<RemoveDataverseMemberRequest>
{
    private readonly DataverseConfigurationProvider _dataverses;
    private readonly IDataverseAccessPolicy _access;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly ILogger<RemoveDataverseMemberEndpointBase> _logger;

    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <inheritdoc />
    protected RemoveDataverseMemberEndpointBase(
        DataverseConfigurationProvider dataverses,
        IDataverseAccessPolicy access,
        IDataGatewayProvider dataGateways,
        ILogger<RemoveDataverseMemberEndpointBase>? logger = null)
    {
        _dataverses = dataverses;
        _access = access;
        _dataGateways = dataGateways;
        _logger = logger ?? NullLogger<RemoveDataverseMemberEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/dataverses/{Name}/members/{MemberId}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("dataverses:write");
#endif
        Summary(s =>
        {
            s.Summary = "Remove a member from a dataverse";
            s.Description = "Revokes a user's or role's membership of the dataverse.";
        });
        Tags("Dataverses");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(RemoveDataverseMemberRequest req, CancellationToken ct)
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

        var target = new DataStoreTarget(_dataverses.DataStoreName, _dataverses.PathName, "DataverseMember");
        var existingCommand = new QueryCommand<DataverseMemberConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = req.MemberId },
                        new FilterCondition { PropertyName = "DataverseImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataverse.Value.Id },
                        // Without this, removing a member whose role was changed at least once could
                        // retire the stale prior version instead of the current row, leaving the
                        // actual current membership still active and unremoved.
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existing = await Gateway.Execute<IEnumerable<DataverseMemberConfiguration>>(existingCommand, target, ct).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            await SendFailure(500, existing.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        var found = new List<DataverseMemberConfiguration>(existing.Value ?? []);
        if (found.Count == 0)
        {
            var notFound = GenericResult.Failure(
                DataversesResultCodes.ByName("DataverseChildNotFound"), _logger,
                ResultDetails.Create("name", req.Name, "kind", "member", "id", req.MemberId.ToString()));
            await SendFailure(404, notFound.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        var row = found[0];
        row.IsDeleted = true;
        row.IsCurrent = false;
        // Same reason as the attach side's CreateDate stamp: the translator writes every mapped
        // column explicitly, so ModifyDate has to be set here or it inserts as 0001-01-01. (FDW-793)
        row.ModifyDate = DateTimeOffset.UtcNow;

        var updateCommand = new UpdateCommand<DataverseMemberConfiguration>(row)
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
            Title = "Member was not removed",
            Detail = reason,
            Instance = HttpContext.Request.Path.HasValue ? HttpContext.Request.Path.Value : null,
        }, ct);
    }
}
