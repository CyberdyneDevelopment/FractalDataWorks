using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Settings.Configuration;

/// <summary>The contract every TenantSetting implementation carries.</summary>
public interface ITenantSettingImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the TenantSetting TenantId.</summary>
    Guid TenantId { get; set; }

    /// <summary>Gets or sets the TenantSetting SettingName.</summary>
    string SettingName { get; set; }

    /// <summary>Gets or sets the TenantSetting SettingValue.</summary>
    string SettingValue { get; set; }

    /// <summary>Gets or sets the TenantSetting IsActive.</summary>
    bool IsActive { get; set; }
}
