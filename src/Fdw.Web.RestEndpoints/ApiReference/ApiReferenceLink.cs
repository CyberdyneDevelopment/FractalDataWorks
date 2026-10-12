namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>A link to an API reference page the caller may open.</summary>
/// <param name="Title">What the page shows.</param>
/// <param name="Url">Where the page is served.</param>
public sealed record ApiReferenceLink(string Title, string Url);
