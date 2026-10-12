using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Web.Http.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// The identity a tenant run acts as: work a tenant set running -- a pipeline -- acting as no person.
/// </summary>
/// <remarks>
/// <para>
/// It is not the system context: everything that judges a caller judges a tenant run as one. It acts in
/// exactly one tenant, <see cref="ActiveTenantId"/>, and holds exactly the permissions of the roles its
/// tenant recorded for it, resolved when the run starts (FDW-1087). Row-level security admits it to its own
/// tenant's rows through its own branch, because no membership names it.
/// </para>
/// <para>
/// <see cref="UserId"/> is the run's principal -- the pipeline's id -- so execution records, logs and
/// <c>SESSION_CONTEXT('UserId')</c> name what is running.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class TenantRunAuthenticationContext : IAuthenticationContext
{
    /// <summary>Initializes a new instance of the <see cref="TenantRunAuthenticationContext"/> class.</summary>
    /// <param name="principalId">The run's principal: the id of the pipeline that is running.</param>
    /// <param name="principalName">The run's principal's name, for logs.</param>
    /// <param name="tenantId">The tenant the run acts in.</param>
    /// <param name="roles">The names of the roles the tenant recorded for the run.</param>
    /// <param name="permissions">The permissions those roles carry in <paramref name="tenantId"/>.</param>
    public TenantRunAuthenticationContext(
        Guid principalId,
        string principalName,
        Guid tenantId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions)
    {
        if (principalId == Guid.Empty) throw new ArgumentException("A tenant run names its principal.", nameof(principalId));
        if (tenantId == Guid.Empty) throw new ArgumentException("A tenant run names its tenant.", nameof(tenantId));
        PrincipalId = principalId;
        Username = principalName ?? throw new ArgumentNullException(nameof(principalName));
        ActiveTenantId = tenantId;
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }

    /// <summary>Gets the run's principal: the id of the pipeline that is running.</summary>
    public Guid PrincipalId { get; }

    /// <inheritdoc />
    public string UserId => PrincipalId.ToString("D", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string Username { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Claims { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <inheritdoc />
    public IEnumerable<string> Roles { get; }

    /// <inheritdoc />
    public IEnumerable<string> Permissions { get; }

    /// <inheritdoc />
    public bool IsAuthenticated => true;

    /// <inheritdoc />
    public SecurityMethodBase AuthenticationMethod => (SecurityMethodBase)SecurityMethods.ByName("None");

    /// <inheritdoc />
    public DateTimeOffset? ExpiresAt => null;

    /// <inheritdoc />
    public Guid? ActiveTenantId { get; }

    /// <inheritdoc />
    public Guid? ActiveOrgId => null;

    /// <inheritdoc />
    public bool IsSystemContext => false;
}
