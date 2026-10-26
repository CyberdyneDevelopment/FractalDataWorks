using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for reading the caller's own published home page and draft (FDW-2471). It needs no right: everyone reads their own page.
/// </summary>
public abstract class GetPersonHomePageEndpointBase : EndpointWithoutRequest<HomePageResponse>
{
    private readonly IHomePages _pages;
    private readonly IAuthenticationContextAccessor _authContext;

    /// <summary>Initializes a new instance of the <see cref="GetPersonHomePageEndpointBase"/> class.</summary>
    protected GetPersonHomePageEndpointBase(IHomePages pages, IAuthenticationContextAccessor authContext)
    {
        _pages = pages;
        _authContext = authContext;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/users/me/home-page");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (PersonHomePageAccess.Of(_authContext) is not { } held)
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
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
