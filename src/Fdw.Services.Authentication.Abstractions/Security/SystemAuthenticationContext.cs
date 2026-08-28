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
/// Why this type exists: <c>security.fn_TenantFilter</c> is unchanged and reads
/// <c>SESSION_CONTEXT('UserId') IS NULL</c> as Mode 1 — a deliberate full-visibility system bypass.
/// The application NEVER sends a null <c>UserId</c> itself: an established context with no
/// Guid-parseable <see cref="UserId"/> resolves instead to the reserved deny-everywhere
/// <see cref="AuthConstants.NoAccessPrincipalId"/> principal (see
/// <c>MsSqlConnection.BuildSessionContextPlan</c>). Host bootstrap/startup reads (resolving
/// <c>IConfigurationGateway</c> before the first request is served), migrations, and seed scripts
/// run outside any HTTP request or tenant-scoped unit of work, so they have no
/// <see cref="System.Security.Claims.ClaimsPrincipal"/> and no per-run <c>TenantId</c> — without
/// this type, those reads would resolve to the deny-everywhere principal above and the application
/// could not read its own connection/data-store catalog to boot. <c>SystemAuthenticationContext</c>
/// gives those call sites a real, explicit <see cref="IAuthenticationContext"/> whose only effect is
/// that <c>MsSqlConnection.SetUserSessionContext</c> sets NOTHING at all — no <c>SESSION_CONTEXT</c>
/// keys whatsoever — which is exactly what the unchanged Mode 1 predicate above requires.
/// </para>
/// <para>
/// This is NOT a fallback: it is never constructed implicitly when an identity is merely absent —
/// that case resolves to the deny-everywhere principal, not this elevation. It must be constructed
/// and assigned to <see cref="IAuthenticationContextAccessor.Current"/> explicitly, at a well-defined
/// point in host bootstrap, and cleared before the host starts accepting requests — see
/// <see cref="SystemAuthenticationContextScope"/> for the sanctioned set/restore mechanism.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SystemAuthenticationContext : IAuthenticationContext
{
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
    public Guid? ActiveTenantId => null;

    /// <inheritdoc/>
    public Guid? ActiveOrgId => null;

    /// <inheritdoc/>
    public bool IsCrossTenant => false;

    /// <inheritdoc/>
    public bool IsSystemContext => true;
}
