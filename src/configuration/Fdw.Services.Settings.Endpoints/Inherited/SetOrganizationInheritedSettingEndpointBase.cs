using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Settings.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for setting an organization's value for a setting (FDW-1579). The caller must hold the right at the organization's scope,
/// and the organization cannot set a setting its tenant or the platform locked.
/// </summary>
public abstract class SetOrganizationInheritedSettingEndpointBase : Endpoint<SetOrganizationInheritedSettingRequest, InheritedSettingResponse>
{
    private readonly IInheritedSettings _settings;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="SetOrganizationInheritedSettingEndpointBase"/> class.</summary>
    protected SetOrganizationInheritedSettingEndpointBase(ILogger logger, IInheritedSettings settings, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _settings = settings;
        _audit = audit;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/organizations/{OrganizationId}/settings/{SettingName}");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SetOrganizationInheritedSettingRequest req, CancellationToken ct)
    {
        var scope = SettingScope.ForOrganization(req.OrganizationId);
        var before = await _settings.Get(scope, req.SettingName, ct).ConfigureAwait(false);
        var set = await _settings.Set(scope, req.SettingName, req.Value, false, ct).ConfigureAwait(false);
        if (set.IsFailure || set.Value is null)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(set, HttpContext)).ConfigureAwait(false);
            return;
        }

        var audited = await InheritedSettingAudit.Record(_audit, "SettingSet", scope.Level, InheritedSettingAudit.ItemOf(scope, req.SettingName), req.OrganizationId,
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
