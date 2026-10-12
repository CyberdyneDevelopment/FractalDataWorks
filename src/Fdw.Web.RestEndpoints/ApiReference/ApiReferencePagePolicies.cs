using Microsoft.Extensions.Configuration;

namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>
/// The policies a caller must hold to open an API reference page, as configured for this host.
/// </summary>
/// <remarks>
/// <para>
/// A page declares its own policies, but a host may replace them without subclassing the page:
/// <c>ApiReference:Pages:{PageName}:RequiredPolicies</c> as a list, where the page name is
/// <c>Name</c> - for environment variables,
/// <c>ApiReference__Pages__DataReference__RequiredPolicies__0=workspaces:read</c>. A page with no
/// such setting keeps its own policies; a setting that is present and empty admits any signed-in caller.
/// </para>
/// <para>
/// The setting changes only the policies. A page that needs no sign-in stays open to anonymous
/// callers, and a page that needs one never becomes anonymous.
/// </para>
/// <para>
/// Every part of the reference that decides who may open a page asks here - the pages list, the page
/// route, and each document endpoint that names its page - so they cannot disagree.
/// </para>
/// </remarks>
public static class ApiReferencePagePolicies
{
    private const string Prefix = "ApiReference:Pages:";
    private const string Suffix = ":RequiredPolicies";

    /// <summary>Gets the configuration key that holds a page's required policies.</summary>
    /// <param name="pageName">The page's name.</param>
    /// <returns>The key, for example <c>ApiReference:Pages:DataReference:RequiredPolicies</c>.</returns>
    public static string KeyFor(string pageName)
        => Prefix + pageName + Suffix;

    /// <summary>Gets the policies a caller must hold to open a page on this host.</summary>
    /// <param name="page">The page.</param>
    /// <param name="configuration">The host's configuration.</param>
    /// <returns>
    /// The configured policies when the host sets any for this page - an empty list when the setting is
    /// present and empty - otherwise the page's own <see cref="IApiReferencePage.RequiredPolicies"/>.
    /// </returns>
    public static IReadOnlyList<string> For(IApiReferencePage page, IConfiguration configuration)
    {
        var section = configuration.GetSection(KeyFor(page.Name));
        if (!section.Exists())
            return page.RequiredPolicies;

        // A list binds as indexed children; a single value (an environment variable set directly, or
        // a setting left empty) holds one policy, a comma-separated few, or none.
        var entries = section.GetChildren().Select(child => child.Value).ToList();
        if (entries.Count == 0 && section.Value is { } single)
            entries.AddRange(single.Split(','));

        return entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Select(entry => entry!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Decides whether a caller may open a page on this host.</summary>
    /// <param name="page">The page.</param>
    /// <param name="caller">Who is asking.</param>
    /// <param name="configuration">The host's configuration.</param>
    /// <returns>Why the caller may not open the page, or <see langword="null"/> when they may.</returns>
    public static string? Refusal(IApiReferencePage page, ApiReferenceCaller caller, IConfiguration configuration)
    {
        return caller.Refusal(!page.RequiresSignIn, For(page, configuration));
    }
}
