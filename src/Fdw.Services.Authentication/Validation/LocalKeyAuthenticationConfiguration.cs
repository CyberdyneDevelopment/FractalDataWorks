using System;
using Fdw.Results;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// The typed body of a <c>LocalKey</c> authentication service — what validating a token this host
/// issued needs beyond the key it was signed with.
/// </summary>
/// <remarks>
/// <para>
/// Audience and nothing else. The obvious move is to reuse
/// <see cref="JwtBearerAuthenticationConfiguration"/>, and it is wrong: that type also requires
/// <c>Roles</c>, and its reason for requiring them does not hold here.
/// </para>
/// <para>
/// A JwtBearer token comes from a remote issuer and carries that issuer's claims, so nothing in it
/// says what the caller may do on this side; the roles are declared against the issuer, and holding
/// a token from it is what confers them. A LocalKey token is one this host minted through its own
/// flow, and the pipeline already resolved that principal's roles and permissions and baked them
/// into the token. Declaring roles against the issuer would confer the same set on every token this
/// host mints, for every user, overriding the per-user answer the flow just computed — which is not
/// a stricter configuration but a quieter authorization bug.
/// </para>
/// <para>
/// Audience stays required. A token is accepted only for the audience it names, so the value here
/// and the one the flow mints are the same fact written twice, and the failure when they disagree
/// is every token being rejected.
/// </para>
/// </remarks>
public sealed class LocalKeyAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="LocalKeyAuthenticationConfiguration"/> class.</summary>
    /// <param name="audience">The audience tokens must name.</param>
    public LocalKeyAuthenticationConfiguration(string audience)
    {
        Audience = audience ?? throw new ArgumentNullException(nameof(audience));
    }

    /// <summary>Gets the audience a token must name to be accepted.</summary>
    public string Audience { get; }

    /// <summary>Reads the typed body of a LocalKey entry.</summary>
    /// <param name="section">The entry's configuration section.</param>
    /// <param name="serviceName">The entry's name, for the failure message.</param>
    /// <param name="logger">The logger.</param>
    public static IGenericResult<LocalKeyAuthenticationConfiguration> Read(
        IConfigurationSection section,
        string serviceName,
        ILogger logger)
    {
        if (section is null) throw new ArgumentNullException(nameof(section));

        return section["Audience"] is { Length: > 0 } audience && !string.IsNullOrWhiteSpace(audience)
            ? GenericResult<LocalKeyAuthenticationConfiguration>.Success(
                new LocalKeyAuthenticationConfiguration(audience))
            : GenericResult<LocalKeyAuthenticationConfiguration>.Failure(
                AuthenticationValidationLog.LocalKeyMissingAudience(logger, serviceName));
    }
}
