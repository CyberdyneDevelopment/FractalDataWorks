using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Web.RestEndpoints.OpenApi;

/// <summary>
/// The one group an operation is listed under in an API reference (FDW-1100).
/// </summary>
/// <remarks>
/// <para>
/// Why one rule here and not a tag on each endpoint: tags came from two places that disagreed. FastEndpoints tagged
/// every operation with its first route segment, title-cased (<c>Datasets</c>, <c>Calculation-Entities</c>,
/// <c>{Workspacename}</c>), and the CRUD bases added their resource name as written (<c>datasets</c>,
/// <c>settings/server</c>). 118 of 337 operations carried both, so a reference listed each twice under two
/// near-identical groups.
/// </para>
/// <para>
/// The group is the resource the URL names: the first literal path segment after <c>api</c> and the version. Only
/// an operation whose path names no resource (a route made only of parameters) takes its first explicit tag.
/// Either way the name is shown the same way: the part before any <c>/</c>, split on <c>-</c>, <c>_</c> and
/// spaces, each word capitalised. An operation with neither is left ungrouped rather than given an invented one.
/// </para>
/// </remarks>
public static class OperationTags
{
    /// <summary>The group the sign-in routes are listed under, whichever path they sit on.</summary>
    public const string Authentication = "Authentication";

    private static readonly string[] AuthenticationSegments = ["auth", "connect"];

    /// <summary>The group an operation is listed under.</summary>
    /// <param name="path">The operation's path as the document lists it.</param>
    /// <param name="explicitTags">The tags the endpoint declared, read only when its path names no resource.</param>
    /// <returns>The group's display name, or null when neither the path nor a tag names one.</returns>
    public static string? For(string path, IEnumerable<string> explicitTags)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(explicitTags);

        var resource = ResourceSegment(path);
        if (resource is not null)
            return AuthenticationSegments.Contains(resource, StringComparer.OrdinalIgnoreCase) ? Authentication : Display(resource);

        var declared = explicitTags.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
        return declared is null ? null : Display(declared);
    }

    /// <summary>A tag as a reference shows it: the part before any '/', its words capitalised and spaced.</summary>
    /// <param name="tag">The tag or path segment.</param>
    /// <returns>The display name.</returns>
    public static string Display(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return string.Join(' ', tag.Split('/')[0].Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }

    private static string? ResourceSegment(string path)
        => path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(IsResource);

    private static bool IsResource(string segment)
        => !string.Equals(segment, "api", StringComparison.OrdinalIgnoreCase)
           && !IsVersion(segment)
           && !segment.StartsWith('{');

    private static bool IsVersion(string segment)
        => segment.Length >= 2 && (segment[0] == 'v' || segment[0] == 'V') && segment[1..].All(char.IsAsciiDigit);
}
