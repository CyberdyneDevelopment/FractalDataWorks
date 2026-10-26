using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for publishing the draft of a tenant's home page (FDW-2471). A tenant that holds no draft is refused; the publish is recorded in the administrative audit.
/// </summary>
public abstract class PublishTenantHomePageEndpointBase : Endpoint<TenantHomePageRequest, HomePageResponse>
{
    private readonly IHomePages _pages;
    private readonly IScopeAuthority _authority;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="PublishTenantHomePageEndpointBase"/> class.</summary>
    protected PublishTenantHomePageEndpointBase(IHomePages pages, IScopeAuthority authority, IAdministrativeAudit audit, ILogger logger)
    {
        _pages = pages;
        _authority = authority;
        _audit = audit;
        EndpointLogger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/tenants/{TenantId}/home-page/publish");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(TenantHomePageRequest req, CancellationToken ct)
    {
        var scope = await TenantHomePageAccess.Check(_authority, req.TenantId, OrganizationHomePageAccess.Write, ct).ConfigureAwait(false);
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
