using Fdw.Collections;
using Fdw.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>
/// Base for the options that declare API reference pages.
/// </summary>
/// <remarks>
/// Identity is the option's full type name, so a page needs no hand-picked id. A page without route
/// parameters links to itself for every caller it admits; a page with them overrides
/// <see cref="Links"/> to offer one link per instance the caller may open. The host may replace the
/// page's <see cref="RequiredPolicies"/> through configuration; see <see cref="ApiReferencePagePolicies"/>.
/// </remarks>
public abstract class ApiReferencePageBase : TypeOptionBase<ApiReferencePageBase>, IApiReferencePage
{
    /// <summary>Initializes a new instance of the <see cref="ApiReferencePageBase"/> class.</summary>
    /// <param name="name">The page's name within the collection.</param>
    /// <param name="route">The route the page is served at.</param>
    /// <param name="title">The page's title.</param>
    /// <param name="requiresSignIn">Whether only a signed-in caller is served the page.</param>
    /// <param name="requiredPolicies">The policies a caller must hold to open the page.</param>
    /// <param name="headerContent">The HTML shown above the reference, or an empty string for none.</param>
    protected ApiReferencePageBase(
        string name,
        string route,
        string title,
        bool requiresSignIn,
        IReadOnlyList<string> requiredPolicies,
        string headerContent)
        : base(name)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        RequiresSignIn = requiresSignIn;
        RequiredPolicies = requiredPolicies ?? throw new ArgumentNullException(nameof(requiredPolicies));
        HeaderContent = headerContent ?? throw new ArgumentNullException(nameof(headerContent));
    }

    /// <inheritdoc />
    public string Route { get; }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public bool RequiresSignIn { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPolicies { get; }

    /// <inheritdoc />
    public string HeaderContent { get; }

    /// <inheritdoc />
    public abstract string DocumentUrl(HttpContext context);

    /// <inheritdoc />
    public virtual Task<IGenericResult<IReadOnlyList<ApiReferenceLink>>> Links(
        ApiReferenceCaller caller, HttpContext context, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApiReferenceLink> links = Admits(caller, context) ? [new ApiReferenceLink(Title, Route)] : [];
        return Task.FromResult(GenericResult<IReadOnlyList<ApiReferenceLink>>.Success(links));
    }

    /// <summary>Decides whether a caller may open the page by the page's own policies.</summary>
    /// <param name="caller">Who is asking.</param>
    /// <returns><see langword="true"/> when the caller may open it.</returns>
    /// <remarks>
    /// Ignores the host's configuration. A page that overrides <see cref="Links"/> calls
    /// <see cref="Admits(ApiReferenceCaller, HttpContext)"/> to honor it.
    /// </remarks>
    protected bool Admits(ApiReferenceCaller caller)
        => caller.Refusal(!RequiresSignIn, RequiredPolicies) is null;

    /// <summary>Decides whether a caller may open the page by the policies this host sets for it.</summary>
    /// <param name="caller">Who is asking.</param>
    /// <param name="context">The request asking; its services hold the host's configuration.</param>
    /// <returns><see langword="true"/> when the caller may open it.</returns>
    protected bool Admits(ApiReferenceCaller caller, HttpContext context)
        => ApiReferencePagePolicies.Refusal(this, caller, context.RequestServices.GetRequiredService<IConfiguration>()) is null;
}
