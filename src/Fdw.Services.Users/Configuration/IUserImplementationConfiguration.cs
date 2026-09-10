using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Models;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Users.Configuration;

/// <summary>The contract every Users implementation carries.</summary>
public interface IUserImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the Users Email.</summary>
    string? Email { get; set; }

    /// <summary>Gets or sets the Users IsActive.</summary>
    bool IsActive { get; set; }

    /// <summary>Gets or sets the Users MustChangePasswordOnLogin.</summary>
    bool MustChangePasswordOnLogin { get; set; }

    /// <summary>Gets or sets the Users LastPasswordChangedAt.</summary>
    DateTimeOffset? LastPasswordChangedAt { get; set; }

    /// <summary>Gets or sets the Users Salt.</summary>
    string? Salt { get; set; }

    /// <summary>Gets or sets the Users AlgorithmName.</summary>
    string? AlgorithmName { get; set; }

    /// <summary>Gets or sets the Users FailedLoginCount.</summary>
    int FailedLoginCount { get; set; }

    /// <summary>Gets or sets the Users LockoutEnd.</summary>
    DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>Gets or sets the Users TenantId.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the Users IsCurrent.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets the Users IsDeleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the Users CreatedAt.</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the Users LastLoginAt.</summary>
    DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>Gets or sets the Users CreateDate.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the Users ModifyDate.</summary>
    DateTimeOffset ModifyDate { get; set; }
}
