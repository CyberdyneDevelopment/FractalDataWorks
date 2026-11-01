using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authorization.Abstractions;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for reading an organization's published home page and its draft (FDW-2325).
/// </summary>
public abstract class GetOrganizationHomePageEndpointBase : Endpoint<OrganizationHomePageRequest, HomePageResponse>
{
    private readonly IHomePages _pages;
    private readonly IOrganizationScope _organizations;

    /// <summary>Initializes a new instance of the <see cref="GetOrganizationHomePageEndpointBase"/> class.</summary>
    protected GetOrganizationHomePageEndpointBase(IHomePages pages, IOrganizationScope organizations)
    {
        _pages = pages;
        _organizations = organizations;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/organizations/{OrganizationId}/home-page");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(OrganizationHomePageRequest req, CancellationToken ct)
    {
        var scope = await OrganizationHomePageAccess.Check(_organizations, req.OrganizationId, OrganizationHomePageAccess.Read, ct).ConfigureAwait(false);
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
