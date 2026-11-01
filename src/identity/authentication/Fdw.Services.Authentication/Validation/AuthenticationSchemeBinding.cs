using System;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// The routing fact one authentication service contributes: the issuer it accepts, and the ASP.NET
/// scheme that validates that issuer's tokens.
/// </summary>
/// <remarks>
/// One of these is registered per enabled <c>AuthenticationServices</c> entry, by the option that
/// declared the scheme. <see cref="IssuerSchemeSelector"/> reads the whole set to route each request.
/// The binding is what keeps issuer selection out of any single mechanism: an option knows its own
/// issuer and its own scheme name and nothing about the others.
/// </remarks>
public sealed class AuthenticationSchemeBinding
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationSchemeBinding"/> class.</summary>
    /// <param name="serviceName">The authentication service's declared name, for diagnostics.</param>
    /// <param name="issuer">The exact <c>iss</c> value this scheme accepts.</param>
    /// <param name="schemeName">The ASP.NET authentication scheme that validates it.</param>
    /// <exception cref="ArgumentException">Thrown when any argument is null, empty or whitespace.</exception>
    public AuthenticationSchemeBinding(string serviceName, string issuer, string schemeName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("An authentication scheme binding must name its service.", nameof(serviceName));
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException("An authentication scheme binding must name the issuer it accepts.", nameof(issuer));
        if (string.IsNullOrWhiteSpace(schemeName))
            throw new ArgumentException("An authentication scheme binding must name its scheme.", nameof(schemeName));

        ServiceName = serviceName;
        Issuer = issuer;
        SchemeName = schemeName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationSchemeBinding"/> class for a scheme
    /// whose credential arrives in a cookie rather than the <c>Authorization</c> header.
    /// </summary>
    /// <param name="serviceName">The authentication service's declared name, for diagnostics.</param>
    /// <param name="issuer">The well-known issuer the scheme is routed under.</param>
    /// <param name="schemeName">The ASP.NET authentication scheme that validates it.</param>
    /// <param name="cookieName">The cookie the credential arrives in.</param>
    /// <exception cref="ArgumentException">Thrown when any argument is null, empty or whitespace.</exception>
    public AuthenticationSchemeBinding(string serviceName, string issuer, string schemeName, string cookieName)
        : this(serviceName, issuer, schemeName)
    {
        if (string.IsNullOrWhiteSpace(cookieName))
            throw new ArgumentException("A cookie-credential binding must name its cookie.", nameof(cookieName));

        CookieName = cookieName;
    }

    /// <summary>Gets the authentication service's declared name.</summary>
    public string ServiceName { get; }

    /// <summary>Gets the exact issuer this scheme accepts.</summary>
    public string Issuer { get; }

    /// <summary>Gets the ASP.NET authentication scheme that validates this issuer's tokens.</summary>
    public string SchemeName { get; }

    /// <summary>
    /// Gets the cookie this scheme's credential arrives in, or null for a scheme whose credential
    /// arrives in the <c>Authorization</c> header.
    /// </summary>
    /// <remarks>
    /// A session cookie names no issuer -- it is a random id -- so <see cref="IssuerSchemeSelector"/>
    /// routes it by which declared cookie the request carries, through this same set of bindings.
    /// </remarks>
    public string? CookieName { get; }
}
