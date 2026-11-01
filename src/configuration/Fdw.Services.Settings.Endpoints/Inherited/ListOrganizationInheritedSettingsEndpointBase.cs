using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for listing the settings as they stand for an organization (FDW-1579). The caller must hold the right at the organization's scope.
/// </summary>
public abstract class ListOrganizationInheritedSettingsEndpointBase : Endpoint<OrganizationInheritedSettingsRequest, InheritedSettingsResponse>
{
    private readonly IInheritedSettings _settings;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="ListOrganizationInheritedSettingsEndpointBase"/> class.</summary>
    protected ListOrganizationInheritedSettingsEndpointBase(ILogger logger, IInheritedSettings settings)
    {
        EndpointLogger = logger;
        _settings = settings;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/organizations/{OrganizationId}/settings");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationInheritedSettingsRequest req, CancellationToken ct)
    {
        var listed = await _settings.List(SettingScope.ForOrganization(req.OrganizationId), ct).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(listed, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(InheritedSettingMap.Of(listed.Value), ct).ConfigureAwait(false);
    }
}
