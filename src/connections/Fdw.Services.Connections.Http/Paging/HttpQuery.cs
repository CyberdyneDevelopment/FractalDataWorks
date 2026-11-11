using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>Sets query parameters on a request address, relative or absolute, keeping every other part of it (FDW-4938).</summary>
public static class HttpQuery
{
    /// <summary>Returns <paramref name="request"/> with each named parameter set to its value, replacing one it already carries.</summary>
    /// <param name="request">The address; relative to a connection's base address, or absolute.</param>
    /// <param name="parameters">The parameters, by the name the remote reads, and their values. A value is escaped; a name is sent as stated.</param>
    /// <returns>The address with the parameters set.</returns>
    /// <remarks>
    /// Why a name is not escaped: the connection states the name exactly as its remote reads it, and some remotes read brackets
    /// (<c>page[number]</c>) that escaping would change.
    /// </remarks>
    public static Uri With(Uri request, params (string Name, string Value)[] parameters)
    {
        var text = request.OriginalString;
        var hash = text.IndexOf('#', StringComparison.Ordinal);
        var fragment = hash >= 0 ? text[hash..] : string.Empty;
        if (hash >= 0) text = text[..hash];

        var question = text.IndexOf('?', StringComparison.Ordinal);
        var path = question >= 0 ? text[..question] : text;
        var pairs = question >= 0
            ? text[(question + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries).ToList()
            : new List<string>();

        foreach (var (name, value) in parameters)
        {
            pairs.RemoveAll(pair => string.Equals(NameOf(pair), name, StringComparison.Ordinal));
            pairs.Add(name + "=" + Uri.EscapeDataString(value));
        }

        var rebuilt = pairs.Count == 0 ? path : path + "?" + string.Join("&", pairs);
        return new Uri(rebuilt + fragment, request.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
    }

    private static string NameOf(string pair)
    {
        var equals = pair.IndexOf('=', StringComparison.Ordinal);
        return Uri.UnescapeDataString(equals >= 0 ? pair[..equals] : pair);
    }
}
