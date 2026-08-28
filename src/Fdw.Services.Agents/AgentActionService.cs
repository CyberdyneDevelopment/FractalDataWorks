using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Agents.Abstractions;
using Fdw.Services.Agents.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Agents;

/// <summary>
/// Implementation of <see cref="IAgentActionService"/> using IDataGateway for persistence.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AgentActionService : IAgentActionService
{
    private readonly IDataGateway _gateway;
    private readonly ILogger<AgentActionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentActionService"/> class.
    /// </summary>
    public AgentActionService(IDataGateway gateway, ILogger<AgentActionService>? logger)
    {
        _gateway = gateway;
        _logger = logger ?? NullLogger<AgentActionService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<AgentActionRecord>>> List(
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        AgentActionLog.ListingAgentActions(_logger, status ?? "all");

        var builder = DataQuery.From<AgentActionRecord>("PlatformConfiguration", "agent", "AgentAction");

        if (!string.IsNullOrWhiteSpace(status))
        {
            builder = builder.Where("Status", status);
        }

        var command = builder.OrderByDescending("RequestedAt").Build();
        var result = await _gateway.Execute<IEnumerable<AgentActionRecord>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<IReadOnlyList<AgentActionRecord>>.Failure(
                AgentActionLog.ListFailed(_logger, result.CurrentMessage ?? "Unknown error"));
        }

        var actions = result.Value!.ToList();
        AgentActionLog.AgentActionsListed(_logger, actions.Count);
        return GenericResult<IReadOnlyList<AgentActionRecord>>.Success(actions);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AgentActionRecord>> Get(
        Guid actionId,
        CancellationToken cancellationToken = default)
    {
        AgentActionLog.FetchingAgentAction(_logger, actionId);

        var command = DataQuery.From<AgentActionRecord>("PlatformConfiguration", "agent", "AgentAction")
            .Where("Id", actionId)
            .Build();

        var result = await _gateway.Execute<IEnumerable<AgentActionRecord>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<AgentActionRecord>.Failure(
                AgentActionLog.FetchFailed(_logger, actionId, result.CurrentMessage ?? "Unknown error"));
        }

        var action = result.Value!.FirstOrDefault();
        if (action is null)
        {
            return GenericResult<AgentActionRecord>.Failure(
                AgentActionLog.AgentActionNotFound(_logger, actionId));
        }

        AgentActionLog.AgentActionRetrieved(_logger, actionId);
        return GenericResult<AgentActionRecord>.Success(action);
    }

    /// <inheritdoc />
    public Task<IGenericResult> Approve(
        Guid actionId,
        string reviewedBy,
        CancellationToken cancellationToken = default)
    {
        return Review(actionId, "Approved", reviewedBy, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult> Deny(
        Guid actionId,
        string reviewedBy,
        CancellationToken cancellationToken = default)
    {
        return Review(actionId, "Denied", reviewedBy, cancellationToken);
    }

    private async Task<IGenericResult> Review(
        Guid actionId,
        string newStatus,
        string reviewedBy,
        CancellationToken cancellationToken)
    {
        AgentActionLog.ReviewingAgentAction(_logger, actionId, newStatus);

        var getResult = await Get(actionId, cancellationToken).ConfigureAwait(false);
        if (!getResult.IsSuccess)
        {
            return GenericResult.Failure(
                AgentActionLog.ReviewTargetNotFound(_logger, actionId));
        }

        var existing = getResult.Value!;
        if (!string.Equals(existing.Status, "Pending", StringComparison.Ordinal))
        {
            return GenericResult.Failure(
                AgentActionLog.ActionAlreadyReviewed(_logger, actionId, existing.Status));
        }

        existing.Status = newStatus;
        existing.ReviewedAt = DateTimeOffset.UtcNow;
        existing.ReviewedBy = reviewedBy;

        var updateCommand = new UpdateCommandBuilder<AgentActionRecord>("AgentAction")
            .DataStore("PlatformConfiguration")
            .Path("agent")
            .Where("Id", actionId)
            .Value(existing);

        var result = await _gateway.Execute<int>(updateCommand, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                AgentActionLog.ReviewFailed(_logger, actionId, newStatus, result.CurrentMessage ?? "Unknown error"));
        }

        AgentActionLog.AgentActionReviewed(_logger, actionId, newStatus, reviewedBy);
        return GenericResult.Success();
    }
}
