using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Web.Http.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// A deliberate, explicit system-elevation <see cref="IAuthenticationContext"/> — the ONLY
/// implementation that reports <see cref="IsSystemContext"/> = <c>true</c>.
/// </summary>
/// <remarks>
/// <para>
/// System context sees and writes every tenant's rows. It is what host startup reads, platform
/// lookups made on a caller's behalf (permissions, signing keys, revocation), and background runs
/// execute as. <c>MsSqlConnection</c> expresses it as <c>SESSION_CONTEXT('System') = '1'</c> — a
/// positive assertion, so a connection that sets no keys at all sees nothing (FDW-825).
/// </para>
/// <para>
/// A system context may carry the tenant it is working for (<see cref="ActiveTenantId"/>): a
/// background run belongs to the tenant that owns its pipeline. That tenant never narrows what the
/// run sees — it decides only which tenant the rows the run writes default to, so a tenant's
/// execution records and outputs land in that tenant rather than in the default tenant.
/// </para>
/// <para>
/// This is NOT a fallback: it is never constructed implicitly when an identity is merely absent —
/// that case resolves to the deny-everywhere <see cref="AuthConstants.NoAccessPrincipalId"/>
/// principal. It must be constructed and assigned to
/// <see cref="IAuthenticationContextAccessor.Current"/> explicitly — see
/// <see cref="SystemAuthenticationContextScope"/> for the sanctioned set/restore mechanism.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SystemAuthenticationContext : IAuthenticationContext
{
    /// <summary>
    /// Initializes a system context that belongs to no tenant — host startup, platform lookups.
    /// </summary>
    public SystemAuthenticationContext()
    {
    }

    /// <summary>
    /// Initializes a system context working for <paramref name="tenantId"/> — a background run of
    /// that tenant's work. Rows it writes default to that tenant; what it sees is not narrowed.
    /// </summary>
    /// <param name="tenantId">The tenant the work belongs to.</param>
    public SystemAuthenticationContext(Guid tenantId)
    {
        ActiveTenantId = tenantId;
    }

    /// <inheritdoc/>
    public string UserId => "system";

    /// <inheritdoc/>
    public string Username => UserId;

    /// <inheritdoc/>
    public IDictionary<string, object> Claims { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <inheritdoc/>
    public IEnumerable<string> Roles { get; } = [];

    /// <inheritdoc/>
    public IEnumerable<string> Permissions { get; } = [];

    /// <inheritdoc/>
    public bool IsAuthenticated => true;

    /// <inheritdoc/>
    public SecurityMethodBase AuthenticationMethod => (SecurityMethodBase)SecurityMethods.ByName("None");

    /// <inheritdoc/>
    public DateTimeOffset? ExpiresAt => null;

    /// <inheritdoc/>
    public Guid? ActiveTenantId { get; }

    /// <inheritdoc/>
    public Guid? ActiveOrgId => null;

    /// <inheritdoc/>
    public bool IsSystemContext => true;
}
