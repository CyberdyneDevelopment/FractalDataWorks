using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for saving the draft of a tenant's home page (FDW-2471). The published page is not touched; the save is recorded in the administrative audit.
/// </summary>
public abstract class SaveTenantHomePageDraftEndpointBase : Endpoint<SaveTenantHomePageDraftRequest, HomePageResponse>
{
    private readonly IHomePages _pages;
    private readonly IScopeAuthority _authority;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="SaveTenantHomePageDraftEndpointBase"/> class.</summary>
    protected SaveTenantHomePageDraftEndpointBase(IHomePages pages, IScopeAuthority authority, IAdministrativeAudit audit, ILogger logger)
    {
        _pages = pages;
        _authority = authority;
        _audit = audit;
        EndpointLogger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/tenants/{TenantId}/home-page/draft");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SaveTenantHomePageDraftRequest req, CancellationToken ct)
    {
        var scope = await TenantHomePageAccess.Check(_authority, req.TenantId, OrganizationHomePageAccess.Write, ct).ConfigureAwait(false);
        if (scope.IsFailure || scope.Value is not { } held)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(scope, HttpContext)).ConfigureAwait(false);
            return;
        }

        var saved = await HomePageChange.Save(_pages, _audit, EndpointLogger, held, req.Page, ct).ConfigureAwait(false);
        if (saved.IsFailure || saved.Value is not { } response)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(saved, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }
}
