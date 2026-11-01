using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for reading the home page that applies to the caller and where it comes from (FDW-2325): their own page, else the
/// organization's they act in, else their tenant's, else none. It needs no right: everyone reads the page that is theirs.
/// </summary>
public abstract class GetHomePageEndpointBase : EndpointWithoutRequest<AppliedHomePageResponse>
{
    private readonly IHomePages _pages;
    private readonly IAuthenticationContextAccessor _authContext;

    /// <summary>Initializes a new instance of the <see cref="GetHomePageEndpointBase"/> class.</summary>
    protected GetHomePageEndpointBase(IHomePages pages, IAuthenticationContextAccessor authContext)
    {
        _pages = pages;
        _authContext = authContext;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/home-page");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (_authContext.Current is not { IsAuthenticated: true, ActiveTenantId: { } tenantId } caller
            || !Guid.TryParse(caller.UserId, out var userId))
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        var applied = await _pages.Applying(userId, tenantId, caller.ActiveOrgId, ct).ConfigureAwait(false);
        if (applied.IsFailure || applied.Value is not { } page)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(applied, HttpContext)).ConfigureAwait(false);
            return;
        }

        var name = page.SourceId?.ToString("D", CultureInfo.InvariantCulture);
        await Send.OkAsync(new AppliedHomePageResponse(new HomePageSourceResponse(page.SourceKind, name), HomePageJson.Of(page.Body)), ct).ConfigureAwait(false);
    }
}
