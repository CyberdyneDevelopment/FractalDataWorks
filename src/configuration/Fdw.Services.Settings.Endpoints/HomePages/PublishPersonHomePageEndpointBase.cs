using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for publishing the draft of the caller's own home page (FDW-2471). It needs no right; a caller that holds no draft is refused.
/// </summary>
public abstract class PublishPersonHomePageEndpointBase : EndpointWithoutRequest<HomePageResponse>
{
    private readonly IHomePages _pages;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="PublishPersonHomePageEndpointBase"/> class.</summary>
    protected PublishPersonHomePageEndpointBase(IHomePages pages, IAuthenticationContextAccessor authContext, IAdministrativeAudit audit, ILogger logger)
    {
        _pages = pages;
        _authContext = authContext;
        _audit = audit;
        EndpointLogger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/users/me/home-page/publish");
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

        var published = await HomePageChange.Publish(_pages, _audit, EndpointLogger, held, ct).ConfigureAwait(false);
        if (published.IsFailure || published.Value is not { } response)
        {
            await Send.ResultAsync(InheritedSettingProblem.Of(published, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }
}
