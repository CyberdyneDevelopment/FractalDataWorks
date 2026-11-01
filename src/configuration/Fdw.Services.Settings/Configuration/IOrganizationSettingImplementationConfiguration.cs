using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Settings.Configuration;

/// <summary>The contract every OrganizationSetting implementation carries.</summary>
public interface IOrganizationSettingImplementationConfiguration : IImplementationConfiguration
{

    /// <summary>Gets or sets the tenant the organization belongs to; it scopes who may read the row.</summary>
    Guid TenantId { get; set; }

    /// <summary>Gets or sets the organization this setting belongs to.</summary>
    Guid OrganizationId { get; set; }

    /// <summary>Gets or sets the OrganizationSetting SettingName.</summary>
    string SettingName { get; set; }

    /// <summary>Gets or sets the OrganizationSetting SettingValue.</summary>
    string SettingValue { get; set; }

    /// <summary>Gets or sets the OrganizationSetting IsActive.</summary>
    bool IsActive { get; set; }

}
