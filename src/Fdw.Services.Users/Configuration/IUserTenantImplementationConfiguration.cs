using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Users.Configuration;

/// <summary>The contract every UserTenants implementation carries.</summary>
public interface IUserTenantImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid UserTenantsId { get; set; }

    /// <summary>Gets or sets the UserTenants UserId.</summary>
    Guid UserId { get; set; }

    /// <summary>Gets or sets the UserTenants TenantId.</summary>
    Guid TenantId { get; set; }

    /// <summary>Gets or sets the UserTenants IsDefault.</summary>
    bool IsDefault { get; set; }

    /// <summary>Gets or sets the UserTenants IsCurrent.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets the UserTenants IsDeleted.</summary>
    bool IsDeleted { get; set; }
}
