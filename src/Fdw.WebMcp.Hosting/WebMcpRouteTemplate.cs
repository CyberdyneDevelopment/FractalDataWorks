using System;
using System.Collections.Generic;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Reads the parameter names out of an ASP.NET route template.
/// </summary>
/// <remarks>
/// Shared because two places need the same answer from the same string: the registry decides whether
/// a tool can be called at all, and the generator builds the URL. Parsing it twice is how the two
/// would come to disagree about what a route needs.
/// </remarks>
public static class WebMcpRouteTemplate
{
    /// <summary>
    /// Gets the parameter names a route template requires, in the order they appear.
    /// </summary>
    /// <param name="route">The route template, for example <c>/connections/{Name}/health</c>.</param>
    /// <returns>The bare parameter names, stripped of constraints, defaults and modifiers.</returns>
    /// <remarks>
    /// A template segment can carry more than a name: <c>{id:int}</c> constrains it, <c>{name?}</c>
    /// marks it optional and <c>{*rest}</c> catches the remainder. Only the name binds to a request
    /// property, so everything from the first ':' onward and the leading '*' are dropped. A default
    /// (<c>{page=1}</c>) is dropped the same way — the value still has to reach the URL, and the
    /// endpoint applies its own default if none arrives.
    /// </remarks>
    public static IReadOnlyList<string> ParameterNames(string route)
    {
        if (string.IsNullOrEmpty(route) || !route.Contains('{', StringComparison.Ordinal))
        {
            return [];
        }

        var names = new List<string>();
        var cursor = 0;

        // Scanned rather than matched with a regex: the grammar is one balanced pair with no
        // nesting, so a pattern buys nothing and brings a ReDoS surface with it.
        while (cursor < route.Length)
        {
            var open = route.IndexOf('{', cursor);
            if (open < 0)
            {
                break;
            }

            var close = route.IndexOf('}', open + 1);
            if (close < 0)
            {
                break;
            }

            var raw = route[(open + 1)..close];
            cursor = close + 1;

            var constraint = raw.IndexOf(':', StringComparison.Ordinal);
            if (constraint >= 0)
            {
                raw = raw[..constraint];
            }

            var fallback = raw.IndexOf('=', StringComparison.Ordinal);
            if (fallback >= 0)
            {
                raw = raw[..fallback];
            }

            raw = raw.TrimStart('*').TrimEnd('?').Trim();

            if (raw.Length > 0)
            {
                names.Add(raw);
            }
        }

        return names;
    }
}
