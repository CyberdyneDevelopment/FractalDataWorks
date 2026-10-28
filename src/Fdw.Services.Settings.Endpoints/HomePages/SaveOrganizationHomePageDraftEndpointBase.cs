using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for saving the draft of an organization's home page (FDW-2325). The published page is not touched; the save is recorded in the administrative audit (FDW-2471).
/// </summary>
public abstract class SaveOrganizationHomePageDraftEndpointBase : Endpoint<SaveOrganizationHomePageDraftRequest, HomePageResponse>
{
    private readonly IHomePages _pages;
    private readonly IOrganizationScope _organizations;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="SaveOrganizationHomePageDraftEndpointBase"/> class.</summary>
    protected SaveOrganizationHomePageDraftEndpointBase(IHomePages pages, IOrganizationScope organizations, IAdministrativeAudit audit, ILogger logger)
    {
        _pages = pages;
        _organizations = organizations;
        _audit = audit;
        EndpointLogger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/organizations/{OrganizationId}/home-page/draft");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SaveOrganizationHomePageDraftRequest req, CancellationToken ct)
    {
        var scope = await OrganizationHomePageAccess.Check(_organizations, req.OrganizationId, OrganizationHomePageAccess.Write, ct).ConfigureAwait(false);
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
