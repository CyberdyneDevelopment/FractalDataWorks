using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for listing the settings as they stand for a tenant (FDW-1579). A platform administrator reads any tenant's;
/// anyone else reads only the tenant they act in.
/// </summary>
public abstract class ListTenantInheritedSettingsEndpointBase : Endpoint<TenantInheritedSettingsRequest, InheritedSettingsResponse>
{
    private readonly IInheritedSettings _settings;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListTenantInheritedSettingsEndpointBase"/> class.</summary>
    protected ListTenantInheritedSettingsEndpointBase(ILogger logger, IInheritedSettings settings, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _settings = settings;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/tenants/{TenantId}/settings");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(TenantInheritedSettingsRequest req, CancellationToken ct)
    {
        if (!InheritedSettingCaller.MayActFor(User, req.TenantId, _systemRoles))
        {
            InheritedSettingsEndpointLog.OtherTenant(EndpointLogger, req.TenantId);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var listed = await _settings.List(SettingScope.ForTenant(req.TenantId), ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(InheritedSettingMap.Of(listed.Value), ct).ConfigureAwait(false);
    }
}
