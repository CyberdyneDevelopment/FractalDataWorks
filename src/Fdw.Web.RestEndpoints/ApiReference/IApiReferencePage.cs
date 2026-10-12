using Fdw.Collections;
using Fdw.Results;
using Microsoft.AspNetCore.Http;

namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>
/// One API reference page: where it is served, who may open it, and which document it shows.
/// </summary>
/// <remarks>
/// Pages are options of <see cref="ApiReferencePages"/>, so the package that declares a page's
/// document endpoint also declares the page, and referencing that package is what serves it.
/// </remarks>
public interface IApiReferencePage : ITypeOption<int, ApiReferencePageBase>
{
    /// <summary>Gets the route the page is served at, for example <c>/api/v1/scalar</c>.</summary>
    /// <remarks>It may carry route parameters, which <see cref="DocumentUrl"/> reads back.</remarks>
    string Route { get; }

    /// <summary>Gets the page's title.</summary>
    string Title { get; }

    /// <summary>Gets a value indicating whether the page is served only to a signed-in caller.</summary>
    bool RequiresSignIn { get; }

    /// <summary>Gets the policies a caller must hold to open the page.</summary>
    IReadOnlyList<string> RequiredPolicies { get; }

    /// <summary>Gets the HTML shown above the reference, or an empty string for none.</summary>
    string HeaderContent { get; }

    /// <summary>Gets the URL the page loads its OpenAPI document from, for this request.</summary>
    /// <param name="context">The request for the page.</param>
    /// <returns>The document's URL.</returns>
    string DocumentUrl(HttpContext context);

    /// <summary>Gets the links to this page a caller may follow.</summary>
    /// <param name="caller">Who is asking.</param>
    /// <param name="context">The request asking.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No link when the caller may not open the page; one per instance for a page with route parameters.</returns>
    Task<IGenericResult<IReadOnlyList<ApiReferenceLink>>> Links(
        ApiReferenceCaller caller, HttpContext context, CancellationToken cancellationToken = default);
}
