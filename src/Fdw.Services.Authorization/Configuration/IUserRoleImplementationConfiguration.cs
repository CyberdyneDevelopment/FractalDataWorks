using System;
using Fdw.Configuration;
using Fdw.Data;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>The contract every UserRole implementation carries.</summary>
public interface IUserRoleImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid UserRoleId { get; set; }

    /// <summary>Gets or sets the UserRole UserId.</summary>
    string UserId { get; set; }

    /// <summary>Gets or sets the UserRole RoleId.</summary>
    Guid RoleId { get; set; }

    /// <summary>Gets or sets the UserRole TenantId.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the UserRole AssignedBy.</summary>
    string? AssignedBy { get; set; }

    /// <summary>Gets or sets the UserRole AssignedAt.</summary>
    DateTimeOffset? AssignedAt { get; set; }

    /// <summary>Gets or sets the UserRole ExpiresAt.</summary>
    DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Gets or sets the UserRole IsCurrent.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets the UserRole IsDeleted.</summary>
    bool IsDeleted { get; set; }
}
