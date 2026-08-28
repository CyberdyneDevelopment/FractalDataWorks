using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// The typed body of a <c>JwtBearer</c> authentication service — what validating a token from a
/// remote issuer needs beyond the issuer itself.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Roles"/> is the authorization half. A token from a remote issuer carries that issuer's
/// claims, not FDW's, so nothing in it says what the caller may do here. The roles are declared on
/// this side, against the issuer: holding a token from THIS issuer, for THIS audience, is what confers
/// them. That is a statement about a machine-to-machine issuer dedicated to one service — the issuer is
/// the identity — and it is why the audience is required rather than optional: without it the roles
/// would be conferred on every token the issuer mints, for any client.
/// </para>
/// <para>
/// The roles are FDW role names. They expand to permissions through <c>authz.RolePermission</c>, the
/// same expansion a signed-in user's roles go through, so a change to what a role grants reaches both
/// at once.
/// </para>
/// </remarks>
public sealed class JwtBearerAuthenticationConfiguration
{
    private JwtBearerAuthenticationConfiguration(string audience, IReadOnlyList<string> roles)
    {
        Audience = audience;
        Roles = roles;
    }

    /// <summary>Gets the audience this scheme requires the token to carry.</summary>
    public string Audience { get; }

    /// <summary>Gets the FDW roles a token validated by this scheme confers.</summary>
    public IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Reads the typed body from the entry's own configuration section.
    /// </summary>
    /// <param name="section">The <c>AuthenticationServices</c> entry.</param>
    /// <param name="serviceName">The entry's declared name, for the reason on failure.</param>
    /// <param name="logger">The logger that carries the reason.</param>
    /// <returns>The typed body, or a failure naming the field that was not declared.</returns>
    public static IGenericResult<JwtBearerAuthenticationConfiguration> Read(
        IConfigurationSection section,
        string serviceName,
        ILogger logger)
    {
        if (section is null) throw new ArgumentNullException(nameof(section));

        var audience = section["Audience"];
        if (string.IsNullOrWhiteSpace(audience))
            return GenericResult<JwtBearerAuthenticationConfiguration>.Failure(
                AuthenticationValidationLog.JwtBearerMissingAudience(logger, serviceName));

        var roles = section.GetSection("Roles").GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToList();

        if (roles.Count == 0)
            return GenericResult<JwtBearerAuthenticationConfiguration>.Failure(
                AuthenticationValidationLog.JwtBearerMissingRoles(logger, serviceName));

        return GenericResult<JwtBearerAuthenticationConfiguration>.Success(
            new JwtBearerAuthenticationConfiguration(audience, roles));
    }
}
