using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// Represents a row from <c>tenant.TenantOrgAccess</c>.
/// Each row grants either a role or a direct permission to a user within a specific
/// tenant-org combination. The authorization service unions these grants into the
/// effective permission set for the current-org tier.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class TenantOrgAccessConfiguration
{
    /// <summary>
    /// Gets or sets the user identifier (matches the <c>sub</c> claim in the JWT).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier this grant belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the org identifier this grant belongs to, or null when the grant is
    /// tenant-wide rather than scoped to one org.
    /// </summary>
    /// <remarks>
    /// Nullable because the column is. Declared as a bare <see cref="Guid"/> it arrived as
    /// <c>Guid.Empty</c> — a value, indistinguishable from a real org id at every call site, and
    /// something each one had to defend itself against. An absence has to be expressible or the
    /// contract cannot state it.
    /// </remarks>
    public Guid? OrgId { get; set; }

    /// <summary>
    /// Gets or sets the visibility group this grant is scoped to, or null when it is not
    /// org-restricted.
    /// </summary>
    /// <remarks>
    /// The column exists on the table and the shipped schema declares it; the record had no
    /// property for it, so nothing could read it. <c>security.fn_TenantFilter</c> reads this
    /// column directly, which is why it is on the row at all.
    /// </remarks>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>
    /// Gets or sets the role name this grant records.
    /// </summary>
    /// <remarks>
    /// Not nullable: the column is NOT NULL, so a null could never arrive. Declaring it nullable
    /// made call sites guard a state the table cannot produce, one line above where the state that
    /// does occur — a name outside the role catalogue — went unguarded.
    /// </remarks>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bare permission name this grant confers.
    /// </summary>
    /// <remarks>Not nullable, for the same reason as <see cref="RoleName"/>: the column is NOT NULL.</remarks>
    public string PermissionName { get; set; } = string.Empty;
}
