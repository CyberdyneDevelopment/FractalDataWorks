using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// One entry of the host's <c>AuthenticationServices</c> array — the header every inbound-token
/// validation mechanism shares, whatever mechanism it names.
/// </summary>
/// <remarks>
/// <para>
/// This is the same header/typed-body split every polymorphic FDW domain uses; the store is the host's
/// own configuration rather than a ConfigurationDb table, because the SET of authentication schemes has
/// to be known while the service collection is still open. A scheme is added to
/// <c>AuthenticationBuilder</c> before <c>Build()</c>; a value read afterwards can fill a scheme's
/// options but cannot bring a scheme into existence. So the schemes are declared where the host's other
/// pre-Build declarations live, and each option binds its own typed body off the same entry.
/// </para>
/// <para>
/// <see cref="Authority"/> is the token issuer this scheme accepts — the <c>iss</c> value, matched
/// exactly. It is what lets a host trust more than one issuer at once and still route each token to the
/// one scheme that can validate it. It is normalised to its absolute form on the way in, because that
/// is the form an issuer puts in the claim: OpenIddict reports
/// <c>https://host/</c> for an authority written <c>https://host</c>, and a declaration matched
/// verbatim against the claim would miss by the trailing slash and reject every token from the issuer
/// it was written for.
/// </para>
/// </remarks>
public sealed record AuthenticationServiceConfiguration
{
    /// <summary>The configuration section holding the array of these.</summary>
    public const string SectionName = "AuthenticationServices";

    /// <summary>Gets the name of this authentication service, unique within the host.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the mechanism that validates tokens from this issuer — an option name of <c>AuthenticationServiceTypes</c>.</summary>
    public string? ServiceOptionType { get; init; }

    /// <summary>Gets whether this authentication service participates in this host.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets the issuer whose tokens this service validates, absolute — matched against the token's <c>iss</c>.</summary>
    public string? Authority { get; init; }

    /// <summary>
    /// Reads the enabled entries of <see cref="SectionName"/> that name <paramref name="serviceOptionType"/>,
    /// paired with the section each was read from so the option can bind its own typed body.
    /// </summary>
    /// <param name="configuration">The host's configuration.</param>
    /// <param name="serviceOptionType">The option name to select on.</param>
    /// <param name="logger">The logger that carries the reason on failure.</param>
    /// <returns>
    /// The matching entries, which may legitimately be none — a host declaring only OpenIddict has no
    /// JwtBearer entry. Failure when an entry is declared but incomplete: a missing Name, ServiceOptionType
    /// or Authority is a half-written declaration, and a scheme built on one accepts tokens nobody meant it to.
    /// </returns>
    public static IGenericResult<IReadOnlyList<(AuthenticationServiceConfiguration Header, IConfigurationSection Section)>> Read(
        IConfiguration configuration,
        string serviceOptionType,
        ILogger logger)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var matches = new List<(AuthenticationServiceConfiguration, IConfigurationSection)>();

        foreach (var section in configuration.GetSection(SectionName).GetChildren())
        {
            var header = new AuthenticationServiceConfiguration
            {
                Name = section["Name"],
                ServiceOptionType = section["ServiceOptionType"],
                Enabled = string.Equals(section["Enabled"], "true", StringComparison.OrdinalIgnoreCase),
                Authority = section["Authority"],
            };

            if (string.IsNullOrWhiteSpace(header.ServiceOptionType))
                return GenericResult<IReadOnlyList<(AuthenticationServiceConfiguration, IConfigurationSection)>>.Failure(
                    AuthenticationValidationLog.EntryMissingServiceOptionType(logger, section.Path));

            if (!string.Equals(header.ServiceOptionType, serviceOptionType, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!header.Enabled)
            {
                AuthenticationValidationLog.EntryDisabled(logger, header.Name ?? section.Path, serviceOptionType);
                continue;
            }

            if (string.IsNullOrWhiteSpace(header.Name))
                return GenericResult<IReadOnlyList<(AuthenticationServiceConfiguration, IConfigurationSection)>>.Failure(
                    AuthenticationValidationLog.EntryMissingName(logger, section.Path));

            if (string.IsNullOrWhiteSpace(header.Authority))
                return GenericResult<IReadOnlyList<(AuthenticationServiceConfiguration, IConfigurationSection)>>.Failure(
                    AuthenticationValidationLog.EntryMissingAuthority(logger, header.Name));

            if (!Uri.TryCreate(header.Authority, UriKind.Absolute, out var authority)
                || (!string.Equals(authority.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
                    && !string.Equals(authority.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)))
                return GenericResult<IReadOnlyList<(AuthenticationServiceConfiguration, IConfigurationSection)>>.Failure(
                    AuthenticationValidationLog.AuthorityNotAbsolute(logger, header.Name, header.Authority));

            matches.Add((header with { Authority = authority.AbsoluteUri }, section));
        }

        return GenericResult<IReadOnlyList<(AuthenticationServiceConfiguration, IConfigurationSection)>>.Success(matches);
    }
}
