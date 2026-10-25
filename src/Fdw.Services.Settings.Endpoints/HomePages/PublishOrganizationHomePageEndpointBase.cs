using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for publishing the draft of an organization's home page (FDW-2325). An organization that holds no draft is refused; the publish is recorded in the administrative audit (FDW-2471).
/// </summary>
public abstract class PublishOrganizationHomePageEndpointBase : Endpoint<OrganizationHomePageRequest, HomePageResponse>
{
    private readonly IHomePages _pages;
    private readonly IOrganizationScope _organizations;
    private readonly IScopeAuthority _authority;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="PublishOrganizationHomePageEndpointBase"/> class.</summary>
    protected PublishOrganizationHomePageEndpointBase(IHomePages pages, IOrganizationScope organizations, IScopeAuthority authority, IAdministrativeAudit audit, ILogger logger)
    {
        _pages = pages;
        _organizations = organizations;
        _authority = authority;
        _audit = audit;
        EndpointLogger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/organizations/{OrganizationId}/home-page/publish");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationHomePageRequest req, CancellationToken ct)
    {
        var scope = await OrganizationHomePageAccess.Check(_organizations, _authority, req.OrganizationId, OrganizationHomePageAccess.Write, ct).ConfigureAwait(false);
        if (scope.IsFailure || scope.Value is not { } held)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(scope, HttpContext)).ConfigureAwait(false);
            return;
        }

        var published = await HomePageChange.Publish(_pages, _audit, EndpointLogger, held, ct).ConfigureAwait(false);
        if (published.IsFailure || published.Value is not { } response)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(published, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }
}
