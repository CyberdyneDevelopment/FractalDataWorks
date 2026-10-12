using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>The contract every RolePermission implementation carries.</summary>
public interface IRolePermissionImplementationConfiguration : IImplementationConfiguration
{

    /// <summary>Gets or sets the RolePermission RoleId.</summary>
    Guid RoleId { get; set; }

    /// <summary>Gets or sets the RolePermission PermissionId.</summary>
    Guid PermissionId { get; set; }

    /// <summary>Gets or sets the RolePermission AssignedBy.</summary>
    string? AssignedBy { get; set; }

    /// <summary>Gets or sets the RolePermission AssignedAt.</summary>
    DateTimeOffset? AssignedAt { get; set; }

    /// <summary>Gets or sets the tenant that owns this grant.</summary>
    Guid? TenantId { get; set; }
}
