using System;
using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>The contract every OrganizationRole implementation carries (FDW-1593).</summary>
public interface IOrganizationRoleImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the logical Id of the organization the role belongs to.</summary>
    Guid OrganizationId { get; set; }

    /// <summary>Gets or sets the name people see; unique within the organization.</summary>
    string RoleName { get; set; }

    /// <summary>Gets or sets what the role is for.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets whether the organization was made with this role, which cannot be deleted.</summary>
    bool IsBuiltIn { get; set; }

    /// <summary>Gets or sets whether this is the live version of the role.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether the role has been deleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the permissions the role carries; read when the role is composed with its children.</summary>
    IList<OrganizationRolePermissionConfiguration> Permissions { get; set; }
}
