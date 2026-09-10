using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Settings.Configuration;

/// <summary>The contract every RoleSetting implementation carries.</summary>
public interface IRoleSettingImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the RoleSetting TenantId.</summary>
    Guid TenantId { get; set; }

    /// <summary>Gets or sets the RoleSetting RoleName.</summary>
    string RoleName { get; set; }

    /// <summary>Gets or sets the RoleSetting SettingName.</summary>
    string SettingName { get; set; }

    /// <summary>Gets or sets the RoleSetting SettingValue.</summary>
    string SettingValue { get; set; }

    /// <summary>Gets or sets the RoleSetting IsActive.</summary>
    bool IsActive { get; set; }
}
