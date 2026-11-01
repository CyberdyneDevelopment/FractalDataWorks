using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization.Abstractions;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for reading a tenant's published home page and its draft (FDW-2471).
/// </summary>
public abstract class GetTenantHomePageEndpointBase : Endpoint<TenantHomePageRequest, HomePageResponse>
{
    private readonly IHomePages _pages;
    private readonly IScopeAuthority _authority;

    /// <summary>Initializes a new instance of the <see cref="GetTenantHomePageEndpointBase"/> class.</summary>
    protected GetTenantHomePageEndpointBase(IHomePages pages, IScopeAuthority authority)
    {
        _pages = pages;
        _authority = authority;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/tenants/{TenantId}/home-page");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(TenantHomePageRequest req, CancellationToken ct)
    {
        var scope = await TenantHomePageAccess.Check(_authority, req.TenantId, OrganizationHomePageAccess.Read, ct).ConfigureAwait(false);
        if (scope.IsFailure || scope.Value is not { } held)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(scope, HttpContext)).ConfigureAwait(false);
            return;
        }

        var read = await HomePageChange.Read(_pages, held, ct).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } response)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(read, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }
}
