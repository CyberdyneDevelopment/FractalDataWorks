using System.Collections.Generic;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// The headers a tool call carries, decided once for every transport.
/// </summary>
/// <remarks>
/// Takes the header name and key already resolved rather than the options, so that choosing them
/// stays with the caller that owns the options and this type decides only which headers a call
/// sends and in what order. A browser agent and a headless one must present the same key, or the
/// same call is authorised through one transport and refused through the other.
/// </remarks>
public static class WebMcpRequestHeaders
{
    /// <summary>Returns the headers for a call, in the order they are sent.</summary>
    /// <param name="withBody">Whether the call carries a JSON body.</param>
    /// <param name="apiKeyHeader">The header that carries the client key.</param>
    /// <param name="clientApiKey">The client key, or <see langword="null"/> when none is configured.</param>
    /// <returns>The header name/value pairs.</returns>
    /// <remarks>
    /// Takes the body shape rather than a verb because the recovery call is always a GET, even on a
    /// tool that writes; reading the tool's verb put Content-Type on a bodyless GET.
    /// </remarks>
    public static IReadOnlyList<KeyValuePair<string, string>> For(
        bool withBody,
        string apiKeyHeader,
        string? clientApiKey)
    {
        var headers = new List<KeyValuePair<string, string>> { new("Accept", "application/json") };

        if (withBody)
        {
            headers.Add(new("Content-Type", "application/json"));
        }

        if (!string.IsNullOrEmpty(clientApiKey))
        {
            headers.Add(new(apiKeyHeader, clientApiKey));
        }

        return headers;
    }
}
