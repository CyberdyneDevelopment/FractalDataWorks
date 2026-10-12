using System;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Reads the bearer token a request carries. Shared by every <see cref="Microsoft.AspNetCore.Authentication.IAuthenticationHandler"/>
/// FDW implements directly, since each reads its own configuration provider rather than
/// <c>IOptionsMonitor&lt;TOptions&gt;</c> (see the handlers' own remarks for why) and so does not
/// inherit token extraction from <c>AuthenticationHandler&lt;TOptions&gt;</c> either.
/// </summary>
internal static class BearerTokenReader
{
    private const string HeaderPrefix = "Bearer ";
    private const string HubPathPrefix = "/hubs";
    private const string QueryTokenKey = "access_token";

    /// <summary>
    /// Reads the bearer token from the <c>Authorization</c> header, or — for a real-time hub
    /// connection only — the <c>access_token</c> query string value.
    /// </summary>
    /// <param name="context">The request being authenticated.</param>
    /// <remarks>
    /// The query-string fallback is scoped to <c>/hubs</c> paths only, never the whole API surface:
    /// a browser's WebSocket handshake cannot carry a custom <c>Authorization</c> header, so
    /// SignalR's own JavaScript client sends the token as a query parameter instead (FDW-809 — its
    /// documented mechanism, not an FDW invention). Accepting a token from the query string on any
    /// other route would leak it into server access logs and browser history for no reason a normal
    /// HTTP call has, since every non-hub caller can and must set the header.
    /// </remarks>
    public static string? Read(HttpContext context)
    {
        var header = ReadHeaderOrQueryFallback(context);
        return header.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase)
            ? header[HeaderPrefix.Length..].Trim()
            : null;
    }

    /// <summary>
    /// Reads the raw <c>Authorization</c> header value, or — for a real-time hub connection only —
    /// a <c>"Bearer {token}"</c> string synthesized from the <c>access_token</c> query value, in the
    /// exact shape a real header would carry.
    /// </summary>
    /// <param name="context">The request being authenticated.</param>
    /// <remarks>
    /// <see cref="IssuerSchemeSelector"/> reads the raw header to distinguish an opaque API-key
    /// credential from a JWT before either is decoded — it cannot use <see cref="Read"/>, which
    /// already assumes the <c>Bearer</c> shape and would silently drop an API key. Synthesizing the
    /// header string here, once, is what lets that selector's own prefix/shape checks work unchanged
    /// for a query-carried token exactly as they do for a real header — the alternative was a THIRD
    /// copy of the query-fallback condition, which is what let this gap go unnoticed in the first
    /// place: two of three token readers were fixed for FDW-809 and this one was not, so the scheme
    /// selector routed every hub connection with no header to <see cref="UnmatchedIssuerHandler"/>
    /// regardless of a valid token in the query string.
    /// </remarks>
    public static string ReadHeaderOrQueryFallback(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(header))
            return header;

        if (context.Request.Path.StartsWithSegments(HubPathPrefix, StringComparison.OrdinalIgnoreCase)
            && context.Request.Query.TryGetValue(QueryTokenKey, out var token)
            && token.Count > 0
            && !string.IsNullOrWhiteSpace(token[0]))
        {
            return HeaderPrefix + token[0];
        }

        return header;
    }
}
