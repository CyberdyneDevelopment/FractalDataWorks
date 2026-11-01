using FastEndpoints;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ApiReference.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>
/// Lists the API reference pages the caller may open: <c>GET /reference/pages</c>.
/// </summary>
/// <remarks>
/// Every page registered in <see cref="ApiReferencePages"/> decides for itself whether it admits the
/// caller, and a page with route parameters offers one link per instance - the workspace reference
/// links each workspace the caller may read. The sign-in page reads this after a sign-in to show the
/// caller where they can go. Anonymous, because what it lists already depends on who asks: an
/// anonymous caller is offered only the pages that need no sign-in.
/// </remarks>
public abstract class ListApiReferencePagesEndpointBase : EndpointWithoutRequest<ApiReferencePageListResponse>
{
    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Initializes a new instance of the <see cref="ListApiReferencePagesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="systemRoles">Decides whether the caller is a platform admin.</param>
    protected ListApiReferencePagesEndpointBase(
        ILogger<ListApiReferencePagesEndpointBase> logger,
        ISystemRoleConfiguration systemRoles)
    {
        Logger = logger ?? NullLogger<ListApiReferencePagesEndpointBase>.Instance;
        _systemRoles = systemRoles ?? throw new ArgumentNullException(nameof(systemRoles));
    }

    /// <summary>Gets the logger.</summary>
    protected new ILogger Logger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/reference/pages");
        AllowAnonymous();
        Tags("ApiReference");
        Summary(s =>
        {
            s.Summary = "List the API reference pages you may open";
            s.Description = "Every API reference page this host serves that the caller may open, one link per page, and "
                + "one per instance for a page with route parameters (for example one per workspace the caller may read).";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var caller = ApiReferenceCaller.From(User, _systemRoles);
        var pages = ApiReferencePages.All();
        var links = new List<ApiReferenceLink>();

        foreach (var page in pages)
        {
            var offered = await page.Links(caller, HttpContext, ct).ConfigureAwait(false);
            if (offered.IsFailure || offered.Value is not { } pageLinks)
            {
                var (statusCode, problem) = ResultHttpStatusMapper.Map(offered, HttpContext);
                await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
                return;
            }

            links.AddRange(pageLinks);
        }

        ApiReferenceLog.PagesListed(Logger, links.Count, pages.Count);
        await Send.OkAsync(new ApiReferencePageListResponse { IsSignedIn = caller.IsAuthenticated, Items = links }, ct).ConfigureAwait(false);
    }
}
