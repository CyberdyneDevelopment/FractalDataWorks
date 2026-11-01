using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Generic base endpoint for saving the draft of the caller's own home page (FDW-2471). It needs no right and is not recorded in the administrative audit: the page is the caller's alone.
/// </summary>
public abstract class SavePersonHomePageDraftEndpointBase : Endpoint<SavePersonHomePageDraftRequest, HomePageResponse>
{
    private readonly IHomePages _pages;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IAdministrativeAudit _audit;

    /// <summary>Gets the logger instance.</summary>
    protected ILogger EndpointLogger { get; }

    /// <summary>Initializes a new instance of the <see cref="SavePersonHomePageDraftEndpointBase"/> class.</summary>
    protected SavePersonHomePageDraftEndpointBase(IHomePages pages, IAuthenticationContextAccessor authContext, IAdministrativeAudit audit, ILogger logger)
    {
        _pages = pages;
        _authContext = authContext;
        _audit = audit;
        EndpointLogger = logger;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/users/me/home-page/draft");
        ConfigureEndpoint();
    }

    /// <summary>Override to configure endpoint-specific settings (policy, summary, tags).</summary>
    protected abstract void ConfigureEndpoint();

    /// <inheritdoc />
    public override async Task HandleAsync(SavePersonHomePageDraftRequest req, CancellationToken ct)
    {
        if (PersonHomePageAccess.Of(_authContext) is not { } held)
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
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
