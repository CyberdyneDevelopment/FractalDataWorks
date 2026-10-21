using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for setting a tenant's value for a setting, locking it against the tenant's organizations or leaving it open
/// (FDW-1579). A platform administrator may for any tenant, anyone else for the tenant they act in; a tenant cannot set a setting the
/// platform locked.
/// </summary>
public abstract class SetTenantInheritedSettingEndpointBase : Endpoint<SetTenantInheritedSettingRequest, InheritedSettingResponse>
{
    private readonly IInheritedSettings _settings;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="SetTenantInheritedSettingEndpointBase"/> class.</summary>
    protected SetTenantInheritedSettingEndpointBase(ILogger logger, IInheritedSettings settings, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _settings = settings;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/tenants/{TenantId}/settings/{SettingName}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SetTenantInheritedSettingRequest req, CancellationToken ct)
    {
        if (!InheritedSettingCaller.MayActFor(User, req.TenantId, _systemRoles))
        {
            InheritedSettingsEndpointLog.OtherTenant(EndpointLogger, req.TenantId);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var scope = SettingScope.ForTenant(req.TenantId);
        var before = await _settings.Get(scope, req.SettingName, ct).ConfigureAwait(false);
        var set = await _settings.Set(scope, req.SettingName, req.Value, req.Locked, ct).ConfigureAwait(false);
        if (set.IsFailure || set.Value is null)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(set, HttpContext)).ConfigureAwait(false);
            return;
        }

        var audited = await InheritedSettingAudit.Record(_audit, "SettingSet", scope.Level, InheritedSettingAudit.ItemOf(scope, req.SettingName), null,
            before.Value, set.Value, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            InheritedSettingsEndpointLog.ChangeNotAudited(EndpointLogger, "setting", InheritedSettingAudit.ItemOf(scope, req.SettingName), audited.CurrentMessage);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(InheritedSettingMap.Of(set.Value), ct).ConfigureAwait(false);
    }
}
