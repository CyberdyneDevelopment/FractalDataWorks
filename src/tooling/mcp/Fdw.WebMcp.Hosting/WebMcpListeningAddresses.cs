using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Fdw.WebMcp.Hosting;

/// <summary>Reads which of a server's listening addresses a host can call itself on (FDW-5721).</summary>
internal static class WebMcpListeningAddresses
{
    /// <summary>The addresses among those given that are absolute http or https addresses on a callable host, each once.</summary>
    /// <param name="addresses">The addresses the server reports.</param>
    /// <returns>The usable addresses, in the order given.</returns>
    internal static Uri[] Usable(IEnumerable<string> addresses)
        => [.. addresses
            .Select(static address => Uri.TryCreate(address, UriKind.Absolute, out var parsed) ? parsed : null)
            .OfType<Uri>()
            .Where(static address => IsHttp(address) && !IsWildcard(address))
            .Distinct()];

    /// <summary>Whether an address is http or https.</summary>
    /// <param name="address">The address.</param>
    /// <returns><see langword="true"/> for http and https.</returns>
    internal static bool IsHttp(Uri address)
        => string.Equals(address.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
           || string.Equals(address.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    // A bind to every interface names no address a request can be sent to.
    private static bool IsWildcard(Uri address)
    {
        if (string.Equals(address.Host, "+", StringComparison.Ordinal) || string.Equals(address.Host, "*", StringComparison.Ordinal))
        {
            return true;
        }

        return IPAddress.TryParse(address.Host.Trim('[', ']'), out var ip)
               && (ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any));
    }
}
