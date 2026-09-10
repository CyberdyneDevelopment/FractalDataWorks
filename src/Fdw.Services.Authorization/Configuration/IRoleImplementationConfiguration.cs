using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>The contract every Role implementation carries.</summary>
public interface IRoleImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid RoleId { get; set; }

    /// <summary>Gets or sets the Role DisplayName.</summary>
    string? DisplayName { get; set; }

    /// <summary>Gets or sets the Role Description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the Role IsTenantScoped.</summary>
    bool IsTenantScoped { get; set; }

    /// <summary>Gets or sets the Role TenantId.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the Role ParentRoleId.</summary>
    Guid? ParentRoleId { get; set; }

    /// <summary>Gets or sets the Role SortOrder.</summary>
    int SortOrder { get; set; }

    /// <summary>Gets or sets the Role IsCurrent.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets the Role IsDeleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the Role CreateDate.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the Role ModifyDate.</summary>
    DateTimeOffset ModifyDate { get; set; }
}
