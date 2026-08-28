using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.Http.Abstractions.Security;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// The session a validated token establishes, for the length of one request.
/// </summary>
/// <remarks>
/// <para>
/// Built per request from the token, never carried over from the flow that minted it. Nothing
/// survives issuance but the token itself — which is what makes a revoked principal stop working
/// within the token's lifetime rather than for as long as some server-side session object happens to
/// live.
/// </para>
/// <para>
/// Carries the methods proved and the assurance reached, so an endpoint demanding multi-factor can
/// answer with a step-up challenge rather than a flat refusal.
/// </para>
/// </remarks>
public sealed class ValidatedTokenAuthenticationContext : IAuthenticationContext
{
    private readonly ValidatedToken _token;

    /// <summary>Initializes a new instance of the <see cref="ValidatedTokenAuthenticationContext"/> class.</summary>
    /// <param name="token">What the presented token established.</param>
    public ValidatedTokenAuthenticationContext(ValidatedToken token)
        => _token = token ?? throw new ArgumentNullException(nameof(token));

    /// <inheritdoc />
    public string UserId => _token.PrincipalId.ToString();

    /// <inheritdoc />
    /// <remarks>
    /// Empty. A username is a display concern the token does not carry, and inventing one from the
    /// principal id would put a value in front of people that looks like a name and is not.
    /// </remarks>
    public string Username => string.Empty;

    /// <inheritdoc />
    public IDictionary<string, object> Claims { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <inheritdoc />
    /// <remarks>
    /// Empty. Roles are not in the token — permissions are, baked at issuance. A caller wanting roles
    /// is asking a question the resource cannot answer from a bearer token alone.
    /// </remarks>
    public IEnumerable<string> Roles => [];

    /// <inheritdoc />
    /// <remarks>The scopes the token carries, which are what this request may attempt.</remarks>
    public IEnumerable<string> Permissions => _token.Scopes;

    /// <inheritdoc />
    public bool IsAuthenticated => true;

    /// <inheritdoc />
    /// <remarks>
    /// How the token was presented, which is always a bearer JWT here. Not how its holder originally
    /// authenticated — that is <see cref="AuthenticationMethods"/>, and the two answer different
    /// questions: one is about this request, the other about the login behind it.
    /// </remarks>
    public SecurityMethodBase AuthenticationMethod => (SecurityMethodBase)SecurityMethods.ByName("JWT");

    /// <summary>Gets the methods actually proved during the flow that minted this token — RFC 8176.</summary>
    public IReadOnlyList<string> AuthenticationMethods => _token.AuthenticationMethods;

    /// <summary>Gets what those methods amounted to.</summary>
    /// <remarks>Null when nothing human was proved, as for a workload.</remarks>
    public string? Acr => _token.Acr;

    /// <inheritdoc />
    public DateTimeOffset? ExpiresAt => _token.ExpiresAt;

    /// <inheritdoc />
    public Guid? ActiveTenantId => _token.TenantId;

    /// <inheritdoc />
    /// <remarks>Not carried. An organisation is resolved from the principal, not asserted by a token.</remarks>
    public Guid? ActiveOrgId => null;

    /// <inheritdoc />
    public bool IsCrossTenant => false;

    /// <inheritdoc />
    public bool IsSystemContext => false;
}
