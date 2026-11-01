namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>The API reference pages a caller may open.</summary>
public sealed class ApiReferencePageListResponse
{
    /// <summary>Gets or sets a value indicating whether the caller is signed in.</summary>
    /// <remarks>
    /// Here so a sign-in page can tell whether its session is live without calling a route that needs a
    /// permission of its own: a caller who may only query data may not read their own user record.
    /// </remarks>
    public bool IsSignedIn { get; set; }

    /// <summary>Gets or sets the links, one per page or page instance.</summary>
    public IReadOnlyList<ApiReferenceLink> Items { get; set; } = [];
}
