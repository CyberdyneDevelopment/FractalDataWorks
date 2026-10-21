using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for lifting the lock a tenant holds on a setting, leaving its value (FDW-1579). Only a platform administrator
/// may; a tenant lifts its own lock by setting the value without it.
/// </summary>
public abstract class LiftTenantSettingLockEndpointBase : Endpoint<TenantInheritedSettingKeyRequest, InheritedSettingResponse>
{
    private readonly IInheritedSettings _settings;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="LiftTenantSettingLockEndpointBase"/> class.</summary>
    protected LiftTenantSettingLockEndpointBase(ILogger logger, IInheritedSettings settings, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _settings = settings;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/tenants/{TenantId}/settings/{SettingName}/lock");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(TenantInheritedSettingKeyRequest req, CancellationToken ct)
    {
        if (!_systemRoles.IsPlatformAdmin(User))
        {
            InheritedSettingsEndpointLog.PlatformAdministratorRequired(EndpointLogger, SettingScope.TenantLevel);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var scope = SettingScope.ForTenant(req.TenantId);
        var before = await _settings.Get(scope, req.SettingName, ct).ConfigureAwait(false);
        var lifted = await _settings.Lift(scope, req.SettingName, ct).ConfigureAwait(false);
        if (lifted.IsFailure || lifted.Value is null)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(lifted, HttpContext)).ConfigureAwait(false);
            return;
        }

        var audited = await InheritedSettingAudit.Record(_audit, "SettingLockLifted", SettingScope.PlatformLevel, InheritedSettingAudit.ItemOf(scope, req.SettingName), null,
            before.Value, lifted.Value, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            InheritedSettingsEndpointLog.ChangeNotAudited(EndpointLogger, "lock lift", InheritedSettingAudit.ItemOf(scope, req.SettingName), audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(InheritedSettingMap.Of(lifted.Value), ct).ConfigureAwait(false);
    }
}
