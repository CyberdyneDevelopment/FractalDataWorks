using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Changes who may use each sign-in flow by editing the flow's own configuration.
/// </summary>
/// <remarks>
/// <para>
/// Reads and writes flows through <see cref="IAuthenticationFlowConfigurationProvider"/>, the same
/// provider the flows load from, so a change is versioned like every configuration write. After a write
/// it reloads this host's flows, so the change is enforced here at once; the flow provider validates the
/// changed flow exactly as it does at startup.
/// </para>
/// <para>
/// Edits a copy of the row. The provider hands back a cached instance other requests hold, and a
/// refused or failed write must leave that instance as it was.
/// </para>
/// <para>
/// Refuses only what would leave sign-in inoperable: a limit naming a permission no role carries, and a
/// restrictive change (switching off, limiting) after which the administrator making it has no enabled
/// flow that proves a method they signed in with and admits them. Everything else a flow can express,
/// including several gates or a gate this host cannot configure, stays for configuration to express.
/// </para>
/// </remarks>
public sealed class AuthenticationFlowAdministration : IAuthenticationFlowAdministration
{
    private const string FlowDomain = "AuthenticationFlow";

    private readonly IAuthenticationFlowConfigurationProvider _rows;
    private readonly IAuthenticationFlowProvider _flows;
    private readonly IPermissionConfigurationProvider _permissions;
    private readonly IRolePermissionConfigurationProvider _rolePermissions;
    private readonly ITenantResolver _tenants;
    private readonly IUserPermissions _userPermissions;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly Func<string, IAuthenticationStep?> _step;
    private readonly Func<IEnumerable<IAuthenticationStep>> _steps;
    private readonly ILogger<AuthenticationFlowAdministration> _logger;

    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowAdministration"/> class.</summary>
    /// <param name="rows">Reads and writes the flow rows.</param>
    /// <param name="flows">The flows this host serves, reloaded after every change.</param>
    /// <param name="permissions">The permissions this platform defines, which a gate may name.</param>
    /// <param name="rolePermissions">Which roles carry which permissions, so a limit nobody can meet is refused.</param>
    /// <param name="tenants">Supplies the tenant an administrator's sign-in lands in.</param>
    /// <param name="userPermissions">Resolves what the administrator holds there.</param>
    /// <param name="authContextAccessor">Establishes the trusted context for that read.</param>
    /// <param name="step">How a name becomes a step -- the same lookup the flow provider is handed.</param>
    /// <param name="steps">Every registered step, to find the one administrable gate.</param>
    /// <param name="logger">The logger.</param>
    public AuthenticationFlowAdministration(
        IAuthenticationFlowConfigurationProvider rows,
        IAuthenticationFlowProvider flows,
        IPermissionConfigurationProvider permissions,
        IRolePermissionConfigurationProvider rolePermissions,
        ITenantResolver tenants,
        IUserPermissions userPermissions,
        IAuthenticationContextAccessor authContextAccessor,
        Func<string, IAuthenticationStep?> step,
        Func<IEnumerable<IAuthenticationStep>> steps,
        ILogger<AuthenticationFlowAdministration>? logger = null)
    {
        _rows = rows ?? throw new ArgumentNullException(nameof(rows));
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _rolePermissions = rolePermissions ?? throw new ArgumentNullException(nameof(rolePermissions));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _userPermissions = userPermissions ?? throw new ArgumentNullException(nameof(userPermissions));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _step = step ?? throw new ArgumentNullException(nameof(step));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _logger = logger ?? NullLogger<AuthenticationFlowAdministration>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<AuthenticationFlowState>>> Get(CancellationToken cancellationToken = default)
    {
        var rows = await _rows.Get(cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
            return GenericResult<IReadOnlyList<AuthenticationFlowState>>.Failure(FlowAdministrationLog.FlowsUnreadable(_logger));

        var gate = Gate();
        var states = new List<AuthenticationFlowState>();
        foreach (var row in rows.Value.OrderBy(r => r.Name, StringComparer.Ordinal))
            states.Add(await State(row, gate, cancellationToken).ConfigureAwait(false));

        FlowAdministrationLog.FlowsRead(_logger, states.Count);
        return GenericResult<IReadOnlyList<AuthenticationFlowState>>.Success(states);
    }

    /// <inheritdoc />
    public Task<IGenericResult<AuthenticationFlowState>> SetEnabled(
        string flowName, bool isEnabled, AuthenticationFlowAdministrator administrator, CancellationToken cancellationToken = default)
        => Change(flowName, administrator, restricts: !isEnabled, (flow, _) =>
        {
            if (flow.IsEnabled == isEnabled)
                return GenericResult<bool>.Success(false);

            flow.IsEnabled = isEnabled;
            FlowAdministrationLog.FlowSwitched(_logger, flowName, isEnabled ? "on" : "off", administrator.UserId);
            return GenericResult<bool>.Success(true);
        }, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<AuthenticationFlowState>> RequirePermission(
        string flowName, string permission, AuthenticationFlowAdministrator administrator, CancellationToken cancellationToken = default)
    {
        var defined = await Defined(flowName, permission, cancellationToken).ConfigureAwait(false);
        if (defined.IsFailure)
            return defined.ToNewResult<AuthenticationFlowState>();

        var carried = await Carried(flowName, defined.Value!, cancellationToken).ConfigureAwait(false);
        if (carried.IsFailure)
            return carried.ToNewResult<AuthenticationFlowState>();

        return await Change(flowName, administrator, restricts: true,
            (flow, gate) => Require(flow, gate, defined.Value!.Name, administrator), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IGenericResult<AuthenticationFlowState>> Open(
        string flowName, AuthenticationFlowAdministrator administrator, CancellationToken cancellationToken = default)
        => Change(flowName, administrator, restricts: false, (flow, _) =>
        {
            var gates = Gates(flow.Steps);
            if (gates.Count > 1)
                return Ambiguous(flow.Name, gates.Count);
            if (gates.Count == 0)
                return GenericResult<bool>.Success(false);

            Renumber(flow, flow.Steps.Where(s => !ReferenceEquals(s, gates[0].Row)).OrderBy(s => s.StepOrder).ToList());
            FlowAdministrationLog.FlowOpened(_logger, flow.Name, administrator.UserId);
            return GenericResult<bool>.Success(true);
        }, cancellationToken);

    // One edit, the same way every time: find the row, edit a copy, write it only when something
    // changed, reload this host's flows, and answer with the flow as it now stands.
    private async Task<IGenericResult<AuthenticationFlowState>> Change(
        string flowName,
        AuthenticationFlowAdministrator administrator,
        bool restricts,
        Func<AuthenticationFlowImplementationConfiguration, IGenericResult<IAdministrablePermissionGate>, IGenericResult<bool>> edit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(administrator);

        var rows = await Rows(flowName, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
            return rows.ToNewResult<AuthenticationFlowState>();

        var row = Find(rows.Value!, flowName);
        if (row.IsFailure)
            return row.ToNewResult<AuthenticationFlowState>();

        var gate = Gate();
        var flow = Copy(row.Value!);
        var changed = edit(flow, gate);
        if (changed.IsFailure)
            return changed.ToNewResult<AuthenticationFlowState>();

        if (!changed.Value)
        {
            FlowAdministrationLog.FlowUnchanged(_logger, flowName);
            return GenericResult<AuthenticationFlowState>.Success(await State(flow, gate, cancellationToken).ConfigureAwait(false));
        }

        // Only a change that narrows who may sign in can take the administrator's way in away.
        if (restricts)
        {
            var wayIn = await LeavesAWayIn(administrator, rows.Value!, flow, cancellationToken).ConfigureAwait(false);
            if (wayIn.IsFailure)
                return wayIn.ToNewResult<AuthenticationFlowState>();
            if (!wayIn.Value)
                return GenericResult<AuthenticationFlowState>.Chain(
                    AuthenticationResultCodes.ByName("ChangeLocksOutAdministrator"),
                    GenericResult.Failure(FlowAdministrationLog.ChangeLocksOutAdministrator(
                        _logger, flowName, administrator.UserId, string.Join(", ", administrator.AuthenticationMethods))));
        }

        var saved = await _rows.Update(flow, FlowDomain, flow.Implementation, flow.Name, cancellationToken).ConfigureAwait(false);
        if (saved.IsFailure)
            return GenericResult<AuthenticationFlowState>.Failure(FlowAdministrationLog.FlowSaveFailed(_logger, flowName, saved.CurrentMessage));

        // What makes the change take effect on this host now rather than at its next restart. The flow
        // provider replaces each flow's entry whole, so a sign-in already holding the old flow finishes
        // on it and the next one runs the new.
        var reloaded = await _flows.LoadAndValidate(cancellationToken).ConfigureAwait(false);
        if (reloaded.IsFailure)
            return reloaded.ToNewResult<AuthenticationFlowState>();

        return GenericResult<AuthenticationFlowState>.Success(await State(flow, gate, cancellationToken).ConfigureAwait(false));
    }

    private IGenericResult<bool> Require(
        AuthenticationFlowImplementationConfiguration flow,
        IGenericResult<IAdministrablePermissionGate> gate,
        string permission,
        AuthenticationFlowAdministrator administrator)
    {
        if (gate.IsFailure)
            return GenericResult<bool>.Chain(AuthenticationResultCodes.ByName("FlowCannotBeGated"), gate);

        var configuration = gate.Value!.ConfigurationRequiring(permission);
        var accepted = gate.Value.ValidateConfiguration(configuration);
        if (accepted.IsFailure)
            return accepted.ToNewResult<bool>();

        var gates = Gates(flow.Steps);
        if (gates.Count > 1)
            return Ambiguous(flow.Name, gates.Count);

        if (gates.Count == 1)
        {
            // Another step's settings are not this gate's to write, whatever shape they happen to share.
            if (!string.Equals(gates[0].Row.StepName, gate.Value.Name, StringComparison.Ordinal))
                return GenericResult<bool>.Chain(
                    AuthenticationResultCodes.ByName("FlowGateNotAdministrable"),
                    GenericResult.Failure(FlowAdministrationLog.FlowGateNotAdministrable(_logger, flow.Name, gates[0].Row.StepName)));

            if (string.Equals(gates[0].Row.Configuration, configuration, StringComparison.Ordinal))
                return GenericResult<bool>.Success(false);

            gates[0].Row.Configuration = configuration;
            FlowAdministrationLog.FlowGated(_logger, flow.Name, permission, administrator.UserId);
            return GenericResult<bool>.Success(true);
        }

        var ordered = flow.Steps.OrderBy(s => s.StepOrder).ToList();
        var position = Position(ordered, gate.Value);
        if (position < 0)
            return GenericResult<bool>.Chain(
                AuthenticationResultCodes.ByName("FlowCannotBeGated"),
                GenericResult.Failure(FlowAdministrationLog.FlowCannotBeGated(
                    _logger, flow.Name, string.Join(", ", gate.Value.Requires.Select(r => r.Name)))));

        ordered.Insert(position, new AuthenticationFlowStepConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = gate.Value.Name,
            StepName = gate.Value.Name,
            Configuration = configuration,
        });
        Renumber(flow, ordered);
        FlowAdministrationLog.FlowGated(_logger, flow.Name, permission, administrator.UserId);
        return GenericResult<bool>.Success(true);
    }

    // The first place in the flow where everything the gate reads has been established. Straight after
    // the step that bakes permissions, in every flow that bakes them.
    private int Position(IReadOnlyList<AuthenticationFlowStepConfiguration> ordered, IAuthenticationStep gate)
    {
        var established = new HashSet<IContextElement>();
        for (var i = 0; i < ordered.Count; i++)
        {
            if (gate.Requires.All(established.Contains))
                return i;
            if (_step(ordered[i].StepName) is not { } step)
                return -1;
            established.UnionWith(step.Contributes);
        }

        return gate.Requires.All(established.Contains) ? ordered.Count : -1;
    }

    // Returned as the platform spells it, so the row names the permission exactly as it is baked.
    private async Task<IGenericResult<IPermissionImplementationConfiguration>> Defined(string flowName, string permission, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowName))
            return GenericResult<IPermissionImplementationConfiguration>.Failure(FlowAdministrationLog.NameMissing(_logger));

        var defined = await _permissions.Get(cancellationToken).ConfigureAwait(false);
        if (defined.IsFailure || defined.Value is null)
            return defined.ToNewResult<IPermissionImplementationConfiguration>();

        return defined.Value.FirstOrDefault(p => string.Equals(p.Name, permission, StringComparison.OrdinalIgnoreCase)) is { } found
            ? GenericResult<IPermissionImplementationConfiguration>.Success(found)
            : GenericResult<IPermissionImplementationConfiguration>.Chain(
                AuthenticationResultCodes.ByName("PermissionNotDefined"),
                GenericResult.Failure(FlowAdministrationLog.PermissionNotDefined(_logger, flowName, permission)));
    }

    // A flow limited to a permission no role carries admits nobody, so it is refused until one does.
    private async Task<IGenericResult<bool>> Carried(string flowName, IPermissionImplementationConfiguration permission, CancellationToken cancellationToken)
    {
        var grants = await _rolePermissions.Get(cancellationToken).ConfigureAwait(false);
        if (grants.IsFailure || grants.Value is null)
            return GenericResult<bool>.Failure(FlowAdministrationLog.RoleGrantsUnreadable(_logger, flowName));

        return grants.Value.Any(g => g.PermissionId == permission.Id)
            ? GenericResult<bool>.Success(true)
            : GenericResult<bool>.Chain(
                AuthenticationResultCodes.ByName("PermissionHeldByNoRole"),
                GenericResult.Failure(FlowAdministrationLog.PermissionHeldByNoRole(_logger, flowName, permission.Name)));
    }

    private async Task<IGenericResult<IReadOnlyList<IAuthenticationFlowImplementationConfiguration>>> Rows(string flowName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowName))
            return GenericResult<IReadOnlyList<IAuthenticationFlowImplementationConfiguration>>.Failure(FlowAdministrationLog.NameMissing(_logger));

        var rows = await _rows.Get(cancellationToken).ConfigureAwait(false);
        return rows.IsFailure || rows.Value is null
            ? GenericResult<IReadOnlyList<IAuthenticationFlowImplementationConfiguration>>.Failure(FlowAdministrationLog.FlowsUnreadable(_logger))
            : GenericResult<IReadOnlyList<IAuthenticationFlowImplementationConfiguration>>.Success(rows.Value);
    }

    private IGenericResult<IAuthenticationFlowImplementationConfiguration> Find(
        IReadOnlyList<IAuthenticationFlowImplementationConfiguration> rows, string flowName)
        => rows.FirstOrDefault(r => string.Equals(r.Name, flowName, StringComparison.Ordinal)) is { } row
            ? GenericResult<IAuthenticationFlowImplementationConfiguration>.Success(row)
            : GenericResult<IAuthenticationFlowImplementationConfiguration>.Chain(
                AuthenticationResultCodes.ByName("FlowNotFound"),
                GenericResult.Failure(FlowAdministrationLog.FlowNotConfigured(_logger, flowName)));

    // After this change, is there still an enabled flow proving a method the administrator signed in with
    // that admits them? A caller that did not sign in through a flow (an API key) is not governed by flows,
    // so no flow change can lock it out.
    private async Task<IGenericResult<bool>> LeavesAWayIn(
        AuthenticationFlowAdministrator administrator,
        IReadOnlyList<IAuthenticationFlowImplementationConfiguration> rows,
        AuthenticationFlowImplementationConfiguration changed,
        CancellationToken cancellationToken)
    {
        if (administrator.AuthenticationMethods.Count == 0)
        {
            FlowAdministrationLog.NoFlowSignInToProtect(_logger, administrator.UserId);
            return GenericResult<bool>.Success(true);
        }

        var held = await Held(administrator.UserId, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure)
            return held.ToNewResult<bool>();

        return GenericResult<bool>.Success(rows
            .Select(r => string.Equals(r.Name, changed.Name, StringComparison.Ordinal) ? changed : r)
            .Where(r => r.IsEnabled)
            .Any(r => Admits(r, held.Value!, administrator.AuthenticationMethods)));
    }

    // Proves one of the methods and requires nothing the administrator lacks. A gate whose settings name
    // no permission admits nobody: its flow will not load.
    private bool Admits(IAuthenticationFlowImplementationConfiguration row, HashSet<string> held, IReadOnlyList<string> methods)
        => row.Steps.Select(s => _step(s.StepName)).OfType<IAuthenticationStep>()
               .Any(s => s.AuthenticationMethods.Any(m => methods.Contains(m, StringComparer.Ordinal)))
           && Gates(row.Steps).All(g => g.Step.RequiredPermission(g.Row.Configuration) is { IsSuccess: true, Value: { } p } && held.Contains(p));

    // What the administrator holds in the tenant their sign-in lands in -- the same place a gate reads.
    private async Task<IGenericResult<HashSet<string>>> Held(Guid userId, CancellationToken cancellationToken)
    {
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var tenant = await _tenants.TenantFor(userId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
            return tenant.ToNewResult<HashSet<string>>();

        var resolved = await _userPermissions.Get(userId.ToString(), tenant.Value, orgId: null, cancellationToken).ConfigureAwait(false);
        if (resolved.IsFailure || resolved.Value is not { } authorization)
            return GenericResult<HashSet<string>>.Failure(FlowAdministrationLog.AdministratorPermissionsUnresolved(_logger, userId));

        return GenericResult<HashSet<string>>.Success(new HashSet<string>(authorization.Permissions, StringComparer.OrdinalIgnoreCase));
    }

    // Exactly one step on this host is an administrable gate, or limiting from here is unavailable.
    // Several would leave this service choosing between them, which is not its choice to make.
    private IGenericResult<IAdministrablePermissionGate> Gate()
    {
        var gates = _steps().OfType<IAdministrablePermissionGate>().ToList();
        return gates.Count == 1
            ? GenericResult<IAdministrablePermissionGate>.Success(gates[0])
            : GenericResult<IAdministrablePermissionGate>.Failure(FlowAdministrationLog.GateStepUnavailable(_logger, gates.Count));
    }

    private List<(AuthenticationFlowStepConfiguration Row, IPermissionGatedAuthenticationStep Step)> Gates(
        IEnumerable<AuthenticationFlowStepConfiguration> steps)
        => [.. steps
            .OrderBy(s => s.StepOrder)
            .Select(s => (Row: s, Step: _step(s.StepName) as IPermissionGatedAuthenticationStep))
            .Where(g => g.Step is not null)
            .Select(g => (g.Row, g.Step!))];

    private IGenericResult<bool> Ambiguous(string flowName, int count)
        => GenericResult<bool>.Chain(
            AuthenticationResultCodes.ByName("FlowGateAmbiguous"),
            GenericResult.Failure(FlowAdministrationLog.FlowGateAmbiguous(_logger, flowName, count)));

    private async Task<AuthenticationFlowState> State(
        IAuthenticationFlowImplementationConfiguration row,
        IGenericResult<IAdministrablePermissionGate> gate,
        CancellationToken cancellationToken)
    {
        var ordered = row.Steps.OrderBy(s => s.StepOrder).ToList();
        var gates = Gates(ordered);

        // A switched-off flow is not asked for: the provider would only answer that it is switched off.
        var served = row.IsEnabled ? await _flows.Get(row.Name, cancellationToken).ConfigureAwait(false) : null;

        return new AuthenticationFlowState
        {
            Name = row.Name,
            Description = row.Description,
            IsEnabled = row.IsEnabled,
            IsServing = served is { IsSuccess: true },
            Problem = served is { IsFailure: true } ? served.CurrentMessage : null,
            AuthenticationMethods = [.. ordered
                .Select(s => _step(s.StepName))
                .OfType<IAuthenticationStep>()
                .SelectMany(s => s.AuthenticationMethods)
                .Distinct(StringComparer.Ordinal)],
            // A gate whose settings do not name a permission is left out; the flow then fails to load,
            // and Problem says why.
            RequiredPermissions = [.. gates
                .Select(g => g.Step.RequiredPermission(g.Row.Configuration) is { IsSuccess: true, Value: { } permission } ? permission : null)
                .OfType<string>()],
            CanChangeRequiredPermission = CannotChange(ordered, gates, gate) is null,
            CanChangeRequiredPermissionReason = CannotChange(ordered, gates, gate),
        };
    }

    // The same rules the edits enforce, stated as a sentence a console can show beside a disabled control.
    private string? CannotChange(
        IReadOnlyList<AuthenticationFlowStepConfiguration> ordered,
        List<(AuthenticationFlowStepConfiguration Row, IPermissionGatedAuthenticationStep Step)> gates,
        IGenericResult<IAdministrablePermissionGate> gate)
    {
        if (gate.IsFailure)
            return "This host has no single step that limits a flow by permission.";
        if (gates.Count > 1)
            return "This flow requires several permissions; change it in configuration.";
        if (gates.Count == 1 && !string.Equals(gates[0].Row.StepName, gate.Value!.Name, StringComparison.Ordinal))
            return "This flow is limited by a step this host cannot configure; change it in configuration.";
        if (gates.Count == 0 && Position(ordered, gate.Value!) < 0)
            return "Nothing in this flow establishes the permissions a limit would check.";
        return null;
    }

    // Numbered by position, never re-sorted: a step just inserted has no order yet, and sorting on it
    // would move it to the front of the flow, ahead of the steps it depends on.
    private static void Renumber(AuthenticationFlowImplementationConfiguration flow, List<AuthenticationFlowStepConfiguration> ordered)
    {
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].StepOrder = i + 1;
        flow.Steps = ordered;
    }

    private static AuthenticationFlowImplementationConfiguration Copy(IAuthenticationFlowImplementationConfiguration row)
        => new()
        {
            Id = row.Id,
            Name = row.Name,
            Domain = row.Domain,
            Implementation = row.Implementation,
            Description = row.Description,
            Audience = row.Audience,
            MinimumAcr = row.MinimumAcr,
            ExecutionLifetime = row.ExecutionLifetime,
            SecretManagerName = row.SecretManagerName,
            SecretKeyName = row.SecretKeyName,
            ReturnUrls = row.ReturnUrls,
            CodeLifetime = row.CodeLifetime,
            SessionServiceName = row.SessionServiceName,
            IsEnabled = row.IsEnabled,
            Steps = [.. row.Steps
                .OrderBy(s => s.StepOrder)
                .Select(s => new AuthenticationFlowStepConfiguration
                {
                    Id = s.Id,
                    Name = s.Name,
                    StepOrder = s.StepOrder,
                    StepName = s.StepName,
                    Configuration = s.Configuration,
                })],
        };
}
