using System;
using System.Collections.Generic;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// The origin rules a <c>RequestHost</c> callback is checked by (FDW-5730): what a listed origin is, and when a
/// request's origin is one of them.
/// </summary>
internal static class OidcRedirectOrigins
{
    /// <summary>Splits a comma-separated origin list into its entries.</summary>
    /// <param name="value">The list as the row states it.</param>
    /// <returns>The entries, trimmed, with empty ones dropped.</returns>
    public static string[] Entries(string? value)
        => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Reads one entry as an origin: an absolute http or https scheme://host[:port] and nothing else.</summary>
    /// <param name="entry">The entry as stated.</param>
    /// <returns>The origin, or <see langword="null"/> when the entry is not one.</returns>
    /// <remarks>
    /// A wildcard is refused by its character before parsing: <see cref="Uri"/> accepts <c>*</c> in a host name, and a
    /// listed <c>https://*.example.com</c> would read as one literal host while its author meant every host.
    /// </remarks>
    public static Uri? Origin(string entry)
        => !entry.Contains('*', StringComparison.Ordinal)
            && Uri.TryCreate(entry, UriKind.Absolute, out var parsed)
            && IsHttp(parsed)
            && IsBare(parsed)
                ? parsed
                : null;

    /// <summary>Whether an absolute address carries nothing beyond its scheme, host and port.</summary>
    private static bool IsBare(Uri address)
        => address.UserInfo.Length == 0
            && address.Query.Length == 0
            && address.Fragment.Length == 0
            && string.Equals(address.AbsolutePath, "/", StringComparison.Ordinal);

    /// <summary>Whether an address's scheme is http or https.</summary>
    /// <param name="address">The address.</param>
    /// <returns>Whether it is.</returns>
    public static bool IsHttp(Uri address)
        => string.Equals(address.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.Equals(address.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

    /// <summary>The listed origin the request's origin is, if it is one.</summary>
    /// <param name="allowed">The origins the row lists.</param>
    /// <param name="requested">The request's origin.</param>
    /// <returns>The listed origin, or <see langword="null"/> when the request's origin is not listed.</returns>
    /// <remarks>Scheme, host and port all compared: an http request is not the https origin of the same host.</remarks>
    public static Uri? Listed(IReadOnlyList<Uri> allowed, Uri requested)
    {
        foreach (var origin in allowed)
        {
            if (Uri.Compare(origin, requested, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return origin;
            }
        }

        return null;
    }
}
