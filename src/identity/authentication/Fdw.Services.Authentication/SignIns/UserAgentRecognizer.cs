using System;

namespace Fdw.Services.Authentication.SignIns;

/// <summary>
/// Names the browser and the operating system a User-Agent header describes (FDW-2729).
/// </summary>
/// <remarks>
/// Each table is read in order and the first token found wins, because the strings contain one another: an Edge header also says Chrome and
/// Safari, a Chrome header also says Safari, an iPad header says "like Mac OS X", an Android header says Linux. A header that names nothing
/// known answers null, which the log stores as "not recognised" and keeps the raw string beside. A browser is not looked up by name in a
/// type collection here because the answer depends on the order of the tokens, which a lookup by name does not carry.
/// </remarks>
public static class UserAgentRecognizer
{
    private static readonly (string Token, string Name)[] Browsers =
    [
        ("Edg/", "Edge"),
        ("EdgA/", "Edge"),
        ("EdgiOS/", "Edge"),
        ("Edge/", "Edge"),
        ("OPR/", "Opera"),
        ("Opera", "Opera"),
        ("SamsungBrowser/", "Samsung Internet"),
        ("Firefox/", "Firefox"),
        ("FxiOS/", "Firefox"),
        ("Chrome/", "Chrome"),
        ("CriOS/", "Chrome"),
        ("Safari/", "Safari"),
    ];

    private static readonly (string Token, string Name)[] OperatingSystems =
    [
        ("Windows", "Windows"),
        ("Android", "Android"),
        ("iPhone", "iOS"),
        ("iPad", "iOS"),
        ("iPod", "iOS"),
        ("CrOS", "ChromeOS"),
        ("Mac OS X", "macOS"),
        ("Macintosh", "macOS"),
        ("Linux", "Linux"),
        ("X11", "Linux"),
    ];

    /// <summary>Names the browser a User-Agent header describes.</summary>
    /// <param name="userAgent">The header as sent; null or blank when none was sent.</param>
    /// <returns>The browser; null when the header names none of the known browsers.</returns>
    public static string? BrowserOf(string? userAgent) => FirstNamed(Browsers, userAgent);

    /// <summary>Names the operating system a User-Agent header describes.</summary>
    /// <param name="userAgent">The header as sent; null or blank when none was sent.</param>
    /// <returns>The operating system; null when the header names none of the known systems.</returns>
    public static string? OperatingSystemOf(string? userAgent) => FirstNamed(OperatingSystems, userAgent);

    private static string? FirstNamed((string Token, string Name)[] table, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        foreach (var (token, name) in table)
        {
            if (userAgent.Contains(token, StringComparison.Ordinal))
                return name;
        }

        return null;
    }
}
