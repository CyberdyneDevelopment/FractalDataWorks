using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// What this host asserts about itself when it signs a token.
/// </summary>
/// <remarks>
/// <para>
/// The typed body of an <c>auth.TokenManager</c> header, persisted to <c>auth.JwtTokenManager</c>
/// and joined by <see cref="TokenManagerId"/>. The header carries which secret manager holds the
/// signing key and under what name; this carries the identity that key signs on behalf of.
/// </para>
/// <para>
/// Both properties are nullable and neither has a default. An issuer this host guessed would mint
/// tokens no resource server can match, and a lifetime this host guessed would decide how long a
/// stolen token stays useful — so a missing value fails at the point of use with the row named,
/// rather than resolving to something plausible.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "TokenManager", ServiceType = "Jwt")]
public sealed partial class JwtTokenManagerConfiguration : ITokenManagerImplementationConfiguration
{
    /// <summary>Gets or sets this typed-body row's identifier (<c>auth.JwtTokenManager.Id</c>).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical FK to <c>auth.TokenManager.Id</c>.</summary>
    public Guid TokenManagerId { get; set; }

    /// <summary>
    /// Gets or sets the value minted into <c>iss</c>.
    /// </summary>
    /// <remarks>
    /// Must equal the <c>Authority</c> of the validating host's authentication service entry. The two
    /// are compared as opaque strings, so a trailing slash on one side and not the other is a
    /// mismatch.
    /// </remarks>
    public string? Issuer { get; set; }

    /// <summary>
    /// Gets or sets how long an issued token stays valid, as an ISO 8601 duration (e.g. <c>PT15M</c>).
    /// </summary>
    public string? AccessTokenLifetime { get; set; }
}
