using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for listing the platform's settings, with the lock each carries (FDW-1579). Only a platform administrator may.
/// </summary>
public abstract class ListPlatformInheritedSettingsEndpointBase : EndpointWithoutRequest<InheritedSettingsResponse>
{
    private readonly IInheritedSettings _settings;
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListPlatformInheritedSettingsEndpointBase"/> class.</summary>
    protected ListPlatformInheritedSettingsEndpointBase(ILogger logger, IInheritedSettings settings, ISystemRoleConfiguration systemRoles)
    {
        EndpointLogger = logger;
        _settings = settings;
        _systemRoles = systemRoles;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/platform/settings");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!_systemRoles.IsPlatformAdmin(User))
        {
            InheritedSettingsEndpointLog.PlatformAdministratorRequired(EndpointLogger, SettingScope.PlatformLevel);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var listed = await _settings.List(SettingScope.ForPlatform(), ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(InheritedSettingMap.Of(listed.Value), ct).ConfigureAwait(false);
    }
}
