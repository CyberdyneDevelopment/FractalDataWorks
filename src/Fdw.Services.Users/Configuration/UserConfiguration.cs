using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Users.Models;
using Fdw.Services.Users.Abstractions;

namespace Fdw.Services.Users.Configuration;

/// <summary>
/// Database-backed configuration for users.
/// Maps to <c>usr.Users</c> in ConfigurationDb.
/// </summary>
/// <remarks>
/// Contains identity and security-policy columns but NOT the password hash (the peppered hash
/// lives only in the credential vault — auth.UserSecret on AuthDb). Salt and AlgorithmName are
/// edge-owned metadata, not secrets.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "User")]
public partial class UserConfiguration : IUserConfiguration, IUser
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    // Why: Name = Username for the user domain — the IGenericConfiguration contract requires Name;
    // callers use Username as the domain-specific alias. Both map to the same column.
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName => "Users";

    /// <inheritdoc />
    public string ServiceType => "User";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    // ── Identity columns ──

    /// <summary>Gets or sets the username (alias for <see cref="Name"/>).</summary>
    public string Username
    {
        get => Name;
        set => Name = value;
    }

    /// <summary>Gets or sets the email address.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets whether the user account is active.</summary>
    public bool IsActive { get; set; }

    // ── Security-policy columns (edge-owned, non-secret) ──

    /// <summary>Gets or sets whether the user must change their password on next login.</summary>
    public bool MustChangePasswordOnLogin { get; set; }

    /// <summary>Gets or sets when the user last changed their password.</summary>
    public DateTimeOffset? LastPasswordChangedAt { get; set; }

    /// <summary>Gets or sets the Base64 KDF salt for this user (non-secret, edge-owned).</summary>
    public string? Salt { get; set; }

    /// <summary>Gets or sets the KDF algorithm name used for this user's password hash.</summary>
    public string? AlgorithmName { get; set; }

    /// <summary>Gets or sets the consecutive failed-login counter (reset on success).</summary>
    public int FailedLoginCount { get; set; }

    /// <summary>Gets or sets when a temporary lockout ends, or <c>null</c> if not locked.</summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    // ── Tenant scope ──

    /// <summary>Gets or sets the tenant this user belongs to.</summary>
    public Guid? TenantId { get; set; }

    // ── Soft-delete / version-on-write state ──

    /// <summary>Gets or sets whether this row is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this row is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    // ── Audit columns ──

    /// <summary>Gets or sets when the user record was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the user last logged in.</summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>Gets or sets the audit creation timestamp.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the audit last-modification timestamp.</summary>
    public DateTimeOffset ModifyDate { get; set; }
}
