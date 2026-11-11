using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Disables a connection when a threshold fires with the DisableConnection consequence (FDW-3951).
/// </summary>
/// <remarks>
/// The firing names the connection as its subject, or (FDW-4192) is about a pipeline whose values name the connection its last counted failure came from
/// (<see cref="FailedConnectionValue.Name"/>). It carries the sentence saying which rule matched and the values it
/// named; that sentence is the reason recorded with the disabled state, and the threshold and rule are who recorded it.
/// The state is stamped with the connection's own tenant, read from its row, whichever subject the firing is about, and only the connection's own tenant may disable it:
/// the firing's tenant is handed to <see cref="IConnectionDisabledStates.Disable"/>, which refuses another tenant's connection with a Forbidden failure. The platform may allow a rule owned
/// by the global tenant to disable any tenant's connection (a capability, so <see cref="ConnectionDisablingOption"/> declares it, strict by default, and a host changes it at registration
/// the way FDW-3823's tuning defaults are changed); a tenant that is not the global tenant never may.
/// A connection that is already disabled is left as it is. A failure to record the state fails the consequence, so the
/// firing is reported as not carried out instead of as done.
/// </remarks>
public sealed class ConnectionDisabler : IConnectionDisabler
{
    private readonly IConnectionDisabledStates _states;
    private readonly ITenantProvider _tenants;
    private readonly ConnectionDisablingOption _options;
    private readonly ILogger<ConnectionDisabler> _logger;

    /// <summary>Initializes a new instance of the <see cref="ConnectionDisabler"/> class.</summary>
    /// <param name="states">Where the disabled state of a connection is kept.</param>
    /// <param name="tenants">Tells whether the tenant of a rule is the global tenant.</param>
    /// <param name="options">The option that declares what the platform allows a rule to do; a host changes its defaults at registration.</param>
    /// <param name="logger">The logger.</param>
    public ConnectionDisabler(
        IConnectionDisabledStates states,
        ITenantProvider tenants,
        ConnectionDisablingOption options,
        ILogger<ConnectionDisabler>? logger = null)
    {
        _states = states;
        _tenants = tenants;
        _options = options;
        _logger = logger ?? NullLogger<ConnectionDisabler>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Disable(EscalationFiring firing, CancellationToken cancellationToken = default)
    {
        var connection = ConnectionOf(firing);
        if (connection.IsFailure)
        {
            return connection;
        }

        var mayDisableOthers = await MayDisableOthers(firing, cancellationToken).ConfigureAwait(false);
        if (mayDisableOthers.IsFailure)
        {
            return mayDisableOthers;
        }

        var disabled = await _states.Disable(
            connection.Value,
            firing.Subject.TenantId,
            mayDisableOthers.Value,
            firing.Reason,
            FormattableString.Invariant($"threshold '{firing.ThresholdName}' rule {firing.RuleNumber}"),
            cancellationToken).ConfigureAwait(false);

        return disabled.IsSuccess ? GenericResult.Success() : disabled;
    }

    // Whether the rule's tenant may disable a connection another tenant owns: only when the platform allows it and the tenant is the global tenant. The tenant is read only when the
    // platform allows it at all, so the strict default asks nothing; a tenant that cannot be read fails the consequence and is never assumed to be global or not.
    private async Task<IGenericResult<bool>> MayDisableOthers(EscalationFiring firing, CancellationToken cancellationToken)
    {
        var allowed = _options.GlobalRulesMayDisableOthers(_logger);
        if (allowed.IsFailure)
        {
            return allowed;
        }

        if (!allowed.Value)
        {
            return GenericResult<bool>.Success(false);
        }

        var tenant = await _tenants.GetTenant(firing.Subject.TenantId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure || tenant.Value is null)
        {
            return GenericResult<bool>.Failure(
                ConnectionDisabledLog.RequestingTenantNotRead(_logger, firing.Subject.TenantId, firing.ThresholdName, tenant.NoValueReason().Message));
        }

        return GenericResult<bool>.Success(tenant.Value.IsGlobal);
    }

    // The connection a firing disables: the subject itself when it is a connection; for a pipeline, the connection its values name. Any other subject, or a pipeline
    // whose values name none, is a failure and never a guess.
    private IGenericResult<Guid> ConnectionOf(EscalationFiring firing)
    {
        if (string.Equals(firing.Subject.Kind, ThresholdSubjectKinds.ByName("Connection").Name, StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<Guid>.Success(firing.Subject.SubjectId);
        }

        if (!string.Equals(firing.Subject.Kind, ThresholdSubjectKinds.ByName("Pipeline").Name, StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<Guid>.Failure(ConnectionDisabledLog.SubjectNotAConnection(_logger, firing.ThresholdName, firing.Subject.Kind));
        }

        if (!firing.Values.TryGetValue(FailedConnectionValue.Name, out var named) || named is null)
        {
            return GenericResult<Guid>.Failure(ConnectionDisabledLog.FailedConnectionNotNamed(
                _logger, firing.ThresholdName, firing.Subject.Kind, firing.Subject.Name, FailedConnectionValue.Name));
        }

        return Guid.TryParse(Convert.ToString(named, CultureInfo.InvariantCulture), out var id) && id != Guid.Empty
            ? GenericResult<Guid>.Success(id)
            : GenericResult<Guid>.Failure(ConnectionDisabledLog.FailedConnectionNotAnId(
                _logger, firing.ThresholdName, firing.Subject.Kind, firing.Subject.Name, FailedConnectionValue.Name));
    }
}
