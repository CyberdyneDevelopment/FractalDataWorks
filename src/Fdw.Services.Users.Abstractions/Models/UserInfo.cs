using System;
using Fdw.Data;

namespace Fdw.Services.Users.Models;

/// <summary>
/// User information implementation.
/// </summary>
[GenerateMapper]
public sealed class UserInfo : IUser
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this row is the current version. Cleared on soft-delete so the user
    /// no longer appears in LIST queries that filter IsCurrent=1.
    /// </summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this row is soft-deleted. LIST queries filter it out.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the last login timestamp.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets the account creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the tenant the user belongs to. Resolved from the caller's JWT at
    /// CreateUser time so every user is tenant-scoped; downstream JWT issuance emits this
    /// as the <c>tenant_id</c> claim.
    /// </summary>
    public Guid? TenantId { get; set; }

    // ── Credential security fields (edge-owned; map to usr.Users columns the DDL wave adds) ──

    /// <summary>Gets or sets the Base64 salt for this user's password KDF (edge-owned, not secret).</summary>
    public string? Salt { get; set; }

    /// <summary>Gets or sets the name of the KDF algorithm used for this user's password (for upgrade-on-verify).</summary>
    public string? AlgorithmName { get; set; }

    /// <summary>Gets or sets when the user last changed their password (drives age-based expiry policy).</summary>
    public DateTimeOffset? LastPasswordChangedAt { get; set; }

    /// <summary>Gets or sets whether the user must change their password on next login.</summary>
    public bool MustChangePasswordOnLogin { get; set; }

    /// <summary>Gets or sets the consecutive failed-login counter (reset on success).</summary>
    public int FailedLoginCount { get; set; }

    /// <summary>Gets or sets when a temporary lockout ends, or <c>null</c> if not locked.</summary>
    public DateTimeOffset? LockoutEnd { get; set; }
}
