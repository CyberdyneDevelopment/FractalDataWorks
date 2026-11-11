using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Keeps the disabled state of connections in <c>conn.ConnectionDisabled</c> (FDW-3951), one current row per connection.
/// </summary>
/// <remarks>
/// The state is read from the store every time and never from the configuration cache: a disable written by the ETL host
/// has to stop the API host at once, and the API caches its configuration reads while the ETL host does not. It is one
/// indexed lookup. Disabling and enabling save a new version of the connection's row, so the history stays.
/// </remarks>
public sealed class ConnectionDisabledStates : IConnectionDisabledStates
{
    private const string PathName = "conn";
    private const string ContainerName = "ConnectionDisabled";
    private const string ConnectionContainerName = "Connection";

    private readonly IConfigurationGatewayProvider _gateways;
    private readonly string _dataStoreName;
    private readonly IAuthenticationContextAccessor _accessor;
    private readonly ICrossTenantChangePolicy _crossTenant;
    private readonly TimeProvider _time;
    private readonly ILogger<ConnectionDisabledStates> _logger;

    /// <summary>Initializes a new instance of the <see cref="ConnectionDisabledStates"/> class.</summary>
    /// <param name="gateways">The provider of the configuration gateways.</param>
    /// <param name="dataStoreName">The data store the connection tables live in.</param>
    /// <param name="accessor">The caller's authentication context, which an enable writes under as the tenant the state belongs to (FDW-4385).</param>
    /// <param name="crossTenant">The policy that decides whether an administrator of another tenant may enable the connection (FDW-4459).</param>
    /// <param name="time">The clock a state is stamped with.</param>
    /// <param name="logger">The logger.</param>
    public ConnectionDisabledStates(
        IConfigurationGatewayProvider gateways, string dataStoreName, IAuthenticationContextAccessor accessor, ICrossTenantChangePolicy crossTenant, TimeProvider time, ILogger<ConnectionDisabledStates>? logger = null)
    {
        _gateways = gateways;
        _dataStoreName = dataStoreName;
        _accessor = accessor;
        _crossTenant = crossTenant;
        _time = time;
        _logger = logger ?? NullLogger<ConnectionDisabledStates>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ConnectionDisabledRecord?>> Read(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var gateway = Gateway();
        if (gateway.IsFailure)
        {
            return gateway.ToNewResult<ConnectionDisabledRecord?>();
        }

        var current = await ReadCurrent(gateway.Value!, connectionId, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure)
        {
            return current.ToNewResult<ConnectionDisabledRecord?>();
        }

        return GenericResult<ConnectionDisabledRecord?>.Success(
            current.Value is { EnabledAt: null } disabled ? Record(disabled) : null);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, ConnectionDisabledRecord>>> ReadMany(
        IReadOnlyCollection<Guid> connectionIds, CancellationToken cancellationToken = default)
    {
        if (connectionIds.Count == 0)
        {
            return GenericResult<IReadOnlyDictionary<Guid, ConnectionDisabledRecord>>.Success(new Dictionary<Guid, ConnectionDisabledRecord>());
        }

        var gateway = Gateway();
        if (gateway.IsFailure)
        {
            return gateway.ToNewResult<IReadOnlyDictionary<Guid, ConnectionDisabledRecord>>();
        }

        var command = new QueryCommand<ConnectionDisabledConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "ConnectionId", Operator = FilterOperators.ByName("In"), Value = connectionIds.Cast<object>().ToList() },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var rows = await gateway.Value!.Execute<IEnumerable<ConnectionDisabledConfiguration>>(command, Target(), useCache: false, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
        {
            return GenericResult<IReadOnlyDictionary<Guid, ConnectionDisabledRecord>>.Failure(
                ConnectionDisabledLog.ReadManyFailed(_logger, connectionIds.Count, rows.FailureReason().Message));
        }

        // One current row per connection; a row with an EnabledAt is a connection enabled again.
        IReadOnlyDictionary<Guid, ConnectionDisabledRecord> disabled = (rows.Value ?? [])
            .Where(row => row.EnabledAt is null)
            .ToDictionary(row => row.ConnectionId, Record);
        return GenericResult<IReadOnlyDictionary<Guid, ConnectionDisabledRecord>>.Success(disabled);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ConnectionDisabledRecord>> Disable(
        Guid connectionId, Guid requestingTenantId, bool requesterMayDisableOtherTenants, string reason, string disabledBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return GenericResult<ConnectionDisabledRecord>.Failure(ConnectionDisabledLog.TextMissing(_logger, connectionId, "reason", "disable"));
        }

        if (string.IsNullOrWhiteSpace(disabledBy))
        {
            return GenericResult<ConnectionDisabledRecord>.Failure(ConnectionDisabledLog.TextMissing(_logger, connectionId, "name of who disables it", "disable"));
        }

        var gateway = Gateway();
        if (gateway.IsFailure)
        {
            return gateway.ToNewResult<ConnectionDisabledRecord>();
        }

        // Why the owner is read and checked first (FDW-4192): the escalation service runs a consequence under the system identity, which the row-level security policy lets write any tenant's rows,
        // so the policy cannot refuse a tenant's rule that disables another tenant's connection. Only the connection's own tenant may disable it, unless the caller states that the requester may disable another's (a global-tenant rule the platform allows).
        var owner = await ReadOwner(gateway.Value!, connectionId, cancellationToken).ConfigureAwait(false);
        if (owner.IsFailure || owner.Value is null)
        {
            return owner.ToNewResult<ConnectionDisabledRecord>();
        }

        if (owner.Value.TenantId != requestingTenantId && !requesterMayDisableOtherTenants)
        {
            return GenericResult<ConnectionDisabledRecord>.Failure(
                ConnectionDisabledLog.OtherTenantsConnection(_logger, connectionId, owner.Value.TenantId, requestingTenantId));
        }

        var current = await ReadCurrent(gateway.Value!, connectionId, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure)
        {
            return current.ToNewResult<ConnectionDisabledRecord>();
        }

        if (current.Value is { EnabledAt: null } already)
        {
            ConnectionDisabledLog.AlreadyDisabled(_logger, connectionId);
            return GenericResult<ConnectionDisabledRecord>.Success(Record(already));
        }

        var row = new ConnectionDisabledConfiguration
        {
            Id = current.Value is null ? Guid.CreateVersion7() : current.Value.Id,
            ConnectionId = connectionId,
            DisabledAt = _time.GetUtcNow(),
            DisabledReason = reason,
            DisabledBy = disabledBy,
            TenantId = owner.Value.TenantId,
            VisibilityGroupId = owner.Value.VisibilityGroupId,
            CreateDate = _time.GetUtcNow(),
        };

        var saved = await Save(gateway.Value!, row, current.Value is not null, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            // A write the row-level security policy refuses stays a forbidden failure, with its own code, so the caller can tell it from a fault (FDW-4193).
            return FailureCategory.IsForbidden(saved)
                ? saved.ToNewResult<ConnectionDisabledRecord>()
                : GenericResult<ConnectionDisabledRecord>.Failure(
                    ConnectionDisabledLog.DisableNotSaved(_logger, connectionId, saved.FailureReason().Message));
        }

        ConnectionDisabledLog.Disabled(_logger, connectionId, disabledBy, reason);
        return GenericResult<ConnectionDisabledRecord>.Success(Record(row));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Enable(Guid connectionId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return GenericResult.Failure(ConnectionDisabledLog.TextMissing(_logger, connectionId, "reason", "enable"));
        }

        var gateway = Gateway();
        if (gateway.IsFailure)
        {
            return gateway;
        }

        var current = await ReadCurrent(gateway.Value!, connectionId, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure)
        {
            return current;
        }

        if (current.Value is not { EnabledAt: null } disabled)
        {
            ConnectionDisabledLog.NotDisabled(_logger, connectionId);
            return GenericResult.Success();
        }

        var row = new ConnectionDisabledConfiguration
        {
            Id = disabled.Id,
            ConnectionId = connectionId,
            DisabledAt = disabled.DisabledAt,
            DisabledReason = disabled.DisabledReason,
            DisabledBy = disabled.DisabledBy,
            EnabledAt = _time.GetUtcNow(),
            EnabledReason = reason,
            TenantId = disabled.TenantId,
            VisibilityGroupId = disabled.VisibilityGroupId,
            CreateDate = _time.GetUtcNow(),
        };

        // Why written as the tenant the state belongs to (FDW-4385): the state row is the connection's own tenant's (the disabler stamps it), and an administrator of another tenant
        // who can read the connection may clear it; the row-level security policy refuses a write of another tenant's row by that administrator. The caller has been authorised
        // (the endpoint's policy, and its read of the connection, which returns only one the caller can see); the scope decides only whose row the write belongs to, as when a
        // platform administrator changes a role of another tenant. A state that names no tenant cannot be written as one, and is never given a stand-in.
        if (disabled.TenantId is not { } stateTenant)
        {
            return GenericResult.Failure(ConnectionDisabledLog.StateTenantMissing(_logger, connectionId));
        }

        // Whether the caller's tenant may change another tenant's connection is configuration (FDW-4459): decided here, before the scope below changes whose tenant is in force.
        var permitted = _crossTenant.Permit(CrossTenantChangeOption.ConnectionEnable, stateTenant, _accessor.Current?.ActiveTenantId);
        if (permitted.IsFailure)
        {
            return permitted;
        }

        var enabledBy = _accessor.Current?.UserId ?? LogPlaceholders.NullName;
        IGenericResult<ConfigurationWriteResult> saved;
        using (new SystemAuthenticationContextScope(_accessor, stateTenant))
        {
            saved = await Save(gateway.Value!, row, replacesCurrent: true, cancellationToken).ConfigureAwait(false);
        }

        if (saved.IsFailure)
        {
            // A write the row-level security policy refuses stays a forbidden failure, with our own code and sentence, so the caller can tell it from a fault and is never
            // told the database's text (FDW-4193, FDW-4459).
            return FailureCategory.IsForbidden(saved)
                ? GenericResult.Failure(ConnectionDisabledLog.EnableRefusedBySecurityPolicy(_logger, connectionId))
                : GenericResult.Failure(ConnectionDisabledLog.EnableNotSaved(_logger, connectionId, saved.FailureReason().Message));
        }

        ConnectionDisabledLog.Enabled(_logger, connectionId, enabledBy, reason);
        return GenericResult.Success();
    }

    private IGenericResult<IConfigurationGateway> Gateway()
    {
        var gateway = _gateways.Get(_dataStoreName);
        return gateway.IsSuccess
            ? gateway
            : GenericResult<IConfigurationGateway>.Failure(ConnectionDisabledLog.GatewayUnavailable(_logger, _dataStoreName, gateway.FailureReason().Message));
    }

    private DataStoreTarget Target() => new(_dataStoreName, PathName, ContainerName);

    // The current row of a connection, or nothing when the connection never had one. Read for itself (useCache false).
    private async Task<IGenericResult<ConnectionDisabledConfiguration?>> ReadCurrent(
        IConfigurationGateway gateway, Guid connectionId, CancellationToken cancellationToken)
    {
        var command = new QueryCommand<ConnectionDisabledConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "ConnectionId", Operator = FilterOperators.ByName("Equal"), Value = connectionId },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var rows = await gateway.Execute<IEnumerable<ConnectionDisabledConfiguration>>(command, Target(), useCache: false, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<ConnectionDisabledConfiguration?>.Success((rows.Value ?? []).FirstOrDefault())
            : GenericResult<ConnectionDisabledConfiguration?>.Failure(ConnectionDisabledLog.ReadFailed(_logger, connectionId, rows.FailureReason().Message));
    }

    // The part of the connection's row that says whose it is (its tenant and visibility group): a connection that is not there to this reader is a failure and never a stand-in tenant.
    private async Task<IGenericResult<ConnectionTenantConfiguration>> ReadOwner(IConfigurationGateway gateway, Guid connectionId, CancellationToken cancellationToken)
    {
        var command = new QueryCommand<ConnectionTenantConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = connectionId },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var rows = await gateway.Execute<IEnumerable<ConnectionTenantConfiguration>>(
            command, new DataStoreTarget(_dataStoreName, PathName, ConnectionContainerName), useCache: false, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
        {
            return GenericResult<ConnectionTenantConfiguration>.Failure(ConnectionDisabledLog.TenantNotRead(_logger, connectionId, rows.FailureReason().Message));
        }

        return (rows.Value ?? []).FirstOrDefault() is { } connection
            ? GenericResult<ConnectionTenantConfiguration>.Success(connection)
            : GenericResult<ConnectionTenantConfiguration>.Failure(ConnectionDisabledLog.ConnectionNotFound(_logger, connectionId));
    }

    private Task<IGenericResult<ConfigurationWriteResult>> Save(
        IConfigurationGateway gateway, ConnectionDisabledConfiguration row, bool replacesCurrent, CancellationToken cancellationToken)
        => gateway.Execute<ConfigurationWriteResult>(
            new ConfigurationSaveCommand<ConnectionDisabledConfiguration>(row, replacesCurrent), Target(), cancellationToken);

    private static ConnectionDisabledRecord Record(ConnectionDisabledConfiguration row)
        => new(row.ConnectionId, row.DisabledAt, row.DisabledReason, row.DisabledBy);
}
