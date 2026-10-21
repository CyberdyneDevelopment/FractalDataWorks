using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for setting the platform's value for a setting and locking it against every tenant or leaving it open
/// (FDW-1579). Only a platform administrator may; setting the value without the lock lifts the platform's own lock.
/// </summary>
public abstract class SetPlatformInheritedSettingEndpointBase : Endpoint<SetPlatformInheritedSettingRequest, InheritedSettingResponse>
{
    private readonly IInheritedSettings _settings;
    private readonly IAdministrativeAudit _audit;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="SetPlatformInheritedSettingEndpointBase"/> class.</summary>
    protected SetPlatformInheritedSettingEndpointBase(ILogger logger, IInheritedSettings settings, IAdministrativeAudit audit, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _settings = settings;
        _audit = audit;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/platform/settings/{SettingName}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SetPlatformInheritedSettingRequest req, CancellationToken ct)
    {
        if (!_systemRoles.IsPlatformAdmin(User))
        {
            InheritedSettingsEndpointLog.PlatformAdministratorRequired(EndpointLogger, SettingScope.PlatformLevel);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var scope = SettingScope.ForPlatform();
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
