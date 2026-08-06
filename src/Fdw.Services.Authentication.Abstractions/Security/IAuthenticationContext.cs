using System;
using System.Collections.Generic;
using Fdw.Web.Http.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Represents the authentication context for a user session, containing identity information,
/// roles, claims, and authentication metadata required for authorization decisions.
/// </summary>
/// <remarks>
/// <para>
/// The authentication context serves as the primary data structure for passing
/// authenticated user information throughout the application. It encapsulates
/// all necessary identity and authorization data in a single, immutable context.
/// </para>
/// <para>
/// This context is typically created by IAuthenticationService during the
/// authentication process and consumed by IFrameworkAuthorizationService for making
/// access control decisions.
/// </para>
/// <para>
/// Implementations should be thread-safe and immutable to ensure consistency
/// across concurrent operations and to prevent tampering with authentication data.
/// </para>
/// <para>
/// The context supports various authentication mechanisms through the SecurityMethod
/// enhanced enum system, allowing for flexible authentication provider implementations.
/// </para>
/// </remarks>
public interface IAuthenticationContext
{
    /// <summary>
    /// Gets the unique identifier for the authenticated user.
    /// </summary>
    /// <value>
    /// A unique string identifier for the user, typically a GUID, username, or external system ID.
    /// This value should be stable across sessions and unique within the authentication provider's scope.
    /// </value>
    /// <remarks>
    /// <para>
    /// The user ID serves as the primary key for user identification and should remain
    /// consistent across authentication sessions. It's used for:
    /// - User-specific resource access control
    /// - Audit logging and activity tracking  
    /// - Cross-service user correlation
    /// - Role and permission assignment
    /// </para>
    /// <para>
    /// The format and source of the user ID depends on the authentication provider:
    /// - Database systems typically use numeric or GUID identifiers
    /// - LDAP/Active Directory uses distinguished names or SIDs
    /// - OAuth2 providers use subject identifiers
    /// - Custom systems may use email addresses or usernames
    /// </para>
    /// </remarks>
    string UserId { get; }

    /// <summary>
    /// Gets the human-readable username or display name for the authenticated user.
    /// </summary>
    /// <value>
    /// The username, display name, or email address that identifies the user in a human-readable format.
    /// This value may not be unique across all users but should be meaningful for display purposes.
    /// </value>
    /// <remarks>
    /// <para>
    /// The username provides a user-friendly identifier that can be displayed in
    /// user interfaces, log messages, and audit reports. Unlike UserId, the username
    /// may be changeable and is primarily used for presentation purposes.
    /// </para>
    /// <para>
    /// Common username formats include:
    /// - Login usernames (e.g., "john.doe")
    /// - Email addresses (e.g., "john.doe@company.com")
    /// </para>
    /// <para>
    /// For security logging and audit trails, both UserId and Username should be
    /// recorded to provide complete identification information.
    /// </para>
    /// </remarks>
    string Username { get; }

    /// <summary>
    /// Gets the collection of claims associated with the authenticated user.
    /// </summary>
    /// <value>
    /// A read-only dictionary containing claim names as keys and claim values as objects.
    /// The dictionary may be empty but should never be null.
    /// </value>
    /// <remarks>
    /// <para>
    /// Claims provide additional identity and attribute information about the user
    /// beyond basic identification. They support flexible, extensible user metadata
    /// that can be used for authorization decisions and application logic.
    /// </para>
    /// <para>
    /// Common claim types include:
    /// - Standard identity claims (name, email, phone_number)
    /// - Organizational claims (department, title, employee_id)
    /// - Application-specific claims (subscription_level, preferences)
    /// - Security claims (security_clearance, access_level)
    /// - Temporal claims (session_start, token_issued_at)
    /// </para>
    /// <para>
    /// Claim values are stored as objects to support various data types including
    /// strings, numbers, booleans, dates, and complex objects. Consumers should
    /// perform appropriate type checking and conversion when accessing claim values.
    /// </para>
    /// <para>
    /// The claims collection is typically populated during authentication from
    /// sources such as:
    /// - JWT token claims
    /// - SAML assertions
    /// - LDAP/Active Directory attributes
    /// - Database user profiles
    /// - External identity provider claims
    /// </para>
    /// </remarks>
    IDictionary<string, object> Claims { get; }

    /// <summary>
    /// Gets the collection of roles assigned to the authenticated user.
    /// </summary>
    /// <value>
    /// A read-only enumerable collection of role names assigned to the user.
    /// The collection may be empty but should never be null.
    /// </value>
    /// <remarks>
    /// <para>
    /// Roles provide the primary mechanism for role-based access control (RBAC)
    /// within the application. Each role represents a set of permissions and
    /// capabilities that the user is authorized to exercise.
    /// </para>
    /// <para>
    /// Role assignments can be:
    /// - Direct assignments to the user
    /// - Inherited through group membership
    /// - Derived from organizational hierarchy
    /// - Calculated based on user attributes
    /// </para>
    /// <para>
    /// Common role patterns include:
    /// - Administrative roles ("Administrator", "SystemAdmin")
    /// - Functional roles ("Editor", "Viewer", "Contributor")
    /// - Department-specific roles ("HR.Manager", "Finance.Analyst")
    /// - Application roles ("DataScientist", "ReportUser")
    /// </para>
    /// <para>
    /// The role collection should include all effective roles for the user,
    /// including both directly assigned roles and any inherited roles from
    /// role hierarchies or group memberships.
    /// </para>
    /// </remarks>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Gets the baked effective-permission set carried by the token (one entry per <c>perm</c> claim).
    /// </summary>
    /// <value>
    /// A read-only enumerable of canonical permission strings (e.g. <c>"users:write"</c>) resolved at
    /// token-issue time by the effective-permission resolver and baked into the access token. The
    /// collection may be empty (token carries no <c>perm</c> claims) but is never null.
    /// </value>
    /// <remarks>
    /// Permission baking resolves the full 3-tier union (global/tenant/org) once, at sign-in, and
    /// stamps the result into the access token as <c>perm</c> claims. Stateless enforcement therefore
    /// trusts these claims as authoritative for the token's lifetime rather than re-querying the
    /// authorization store on every request. An empty set means the token grants no permissions.
    /// </remarks>
    IEnumerable<string> Permissions { get; }

    /// <summary>
    /// Gets a value indicating whether the user is currently authenticated.
    /// </summary>
    /// <value>
    /// <c>true</c> if the user is authenticated and the context represents a valid authentication state;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides a quick way to verify that the authentication context
    /// represents a successfully authenticated user. It should return <c>true</c> only
    /// when:
    /// - The user has been successfully authenticated
    /// - The authentication token/credentials are valid
    /// - The authentication has not expired
    /// - The user account is active and not disabled
    /// </para>
    /// <para>
    /// A value of <c>false</c> indicates that the context represents an anonymous
    /// or unauthenticated state, and should not be trusted for authorization decisions.
    /// </para>
    /// <para>
    /// This property is essential for authorization services to quickly validate
    /// that they're working with a valid authentication context before making
    /// access control decisions.
    /// </para>
    /// </remarks>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the authentication method used to authenticate the user.
    /// </summary>
    /// <value>
    /// A AuthenticationMethodBase enhanced enum value indicating the authentication mechanism used
    /// (e.g., JWT, API Key, OAuth2, Basic Authentication).
    /// </value>
    /// <remarks>
    /// <para>
    /// The authentication method provides important context about how the user
    /// was authenticated, which can be used for:
    /// - Security auditing and compliance reporting
    /// - Risk-based access control decisions
    /// - Token handling and validation logic
    /// - Multi-factor authentication requirements
    /// </para>
    /// <para>
    /// Different authentication methods may have different security characteristics:
    /// - JWT tokens provide stateless authentication with embedded claims
    /// - API Keys offer simple but limited authentication for service accounts
    /// - OAuth2 provides delegated authorization with refresh capabilities
    /// - Basic authentication offers simple username/password validation
    /// - Certificate authentication provides high-assurance cryptographic identity
    /// </para>
    /// <para>
    /// Authorization services may use the authentication method to apply
    /// different security policies or access restrictions based on the
    /// assurance level of the authentication mechanism.
    /// </para>
    /// </remarks>
    SecurityMethodBase AuthenticationMethod { get; }

    /// <summary>
    /// Gets the expiration time for the authentication context or associated token.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing when the authentication expires,
    /// or <c>null</c> if the authentication does not expire or expiration is not applicable.
    /// </value>
    /// <remarks>
    /// <para>
    /// The expiration time indicates when the current authentication context
    /// becomes invalid and the user must re-authenticate. This supports security
    /// best practices by limiting the lifetime of authentication credentials.
    /// </para>
    /// <para>
    /// Expiration handling varies by authentication method:
    /// - JWT tokens have embedded expiration times in their claims
    /// - API keys may have configurable expiration policies
    /// - OAuth2 tokens have explicit expiration times
    /// - Session-based authentication may use idle timeouts
    /// - Some authentication methods may not use expiration
    /// </para>
    /// <para>
    /// Applications should check this value when making authorization decisions
    /// and prompt for re-authentication when the context has expired. A <c>null</c>
    /// value indicates that the authentication does not expire through time-based
    /// mechanisms (though it may still be revoked or invalidated).
    /// </para>
    /// <para>
    /// The <see cref="DateTimeOffset"/> type ensures proper timezone handling
    /// for distributed systems operating across multiple time zones.
    /// </para>
    /// </remarks>
    DateTimeOffset? ExpiresAt { get; }

    /// <summary>
    /// Gets the active tenant identifier for this authenticated request.
    /// Set from the <c>tenant_id</c> JWT claim. Null when the token has no tenant scope
    /// (e.g. cross-tenant mode, system connections, or unauthenticated state).
    /// Used by <c>MsSqlConnection</c> to set <c>SESSION_CONTEXT('TenantId')</c> for RLS.
    /// </summary>
    Guid? ActiveTenantId { get; }

    /// <summary>
    /// Gets the active organization identifier for this authenticated request.
    /// Set from the <c>org_id</c> JWT claim. Null when no org context is present.
    /// </summary>
    Guid? ActiveOrgId { get; }

    /// <summary>
    /// Gets a value indicating whether this token was issued with cross-tenant scope.
    /// When true, <c>MsSqlConnection</c> sets <c>SESSION_CONTEXT('CrossTenant')</c> = <c>N'1'</c>,
    /// enabling the RLS cross-tenant mode. Requires the <c>tenants:view-all</c> permission.
    /// Mutually exclusive with a non-null <see cref="ActiveTenantId"/>.
    /// </summary>
    bool IsCrossTenant { get; }

    /// <summary>
    /// Gets a value indicating whether this context represents a deliberate, explicit system
    /// elevation (e.g. host bootstrap/startup reads, migrations, seed scripts) rather than an
    /// authenticated end user or a tenant-scoped unit of work.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, <c>MsSqlConnection</c> sets NOTHING at all — no <c>SESSION_CONTEXT</c> keys
    /// are set on the connection. <c>security.fn_TenantFilter</c> (unchanged) treats a NULL
    /// <c>SESSION_CONTEXT('UserId')</c> as Mode 1 — the full-visibility system bypass — so leaving
    /// the session context untouched IS the elevation; there is no dedicated
    /// <c>SESSION_CONTEXT('SystemContext')</c> key. This is the ONLY path that leaves
    /// <c>UserId</c> unset: every other case (no context, or a context whose <see cref="UserId"/>
    /// is not a parseable <see cref="Guid"/>) resolves instead to the reserved
    /// <see cref="AuthConstants.NoAccessPrincipalId"/> principal — a deliberate deny, never an
    /// implicit "identity absent, therefore system" bypass. Every implementation except
    /// <see cref="SystemAuthenticationContext"/> itself must return <c>false</c>.
    /// </remarks>
    bool IsSystemContext { get; }
}