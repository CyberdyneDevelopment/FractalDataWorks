using System;
using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Options configuration section.
/// </summary>
public sealed class TenantOptionsConfiguration
{
    /// <summary>Gets or sets the maximum number of users.</summary>
    public int? MaxUsers { get; set; }
    /// <summary>Gets or sets the storage quota in bytes.</summary>
    public long? StorageQuotaBytes { get; set; }
    /// <summary>Gets or sets the enabled features.</summary>
    public ICollection<string> EnabledFeatures { get; set; } = new List<string>();
    /// <summary>Gets or sets custom settings.</summary>
    public IDictionary<string, string> CustomSettings { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
    /// <summary>Gets or sets the API rate limit per minute.</summary>
    public int? ApiRateLimitPerMinute { get; set; }

    /// <summary>
    /// Converts to ITenantOptions.
    /// </summary>
    public ITenantOptions ToOptions()
    {
        var options = new TenantOptions
        {
            MaxUsers = MaxUsers,
            StorageQuotaBytes = StorageQuotaBytes,
            ApiRateLimitPerMinute = ApiRateLimitPerMinute
        };

        foreach (var feature in EnabledFeatures)
            options.AddFeature(feature);

        foreach (var setting in CustomSettings)
            options.SetSetting(setting.Key, setting.Value);

        return options;
    }
}
