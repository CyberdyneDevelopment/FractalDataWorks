using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Represents custom options/settings for a tenant.
/// </summary>
public interface ITenantOptions
{
    /// <summary>
    /// Gets the maximum number of users allowed for this tenant.
    /// </summary>
    int? MaxUsers { get; }

    /// <summary>
    /// Gets the storage quota in bytes.
    /// </summary>
    long? StorageQuotaBytes { get; }

    /// <summary>
    /// Gets the enabled feature flags.
    /// </summary>
    IEnumerable<string> EnabledFeatures { get; }

    /// <summary>
    /// Gets custom key-value settings.
    /// </summary>
    IReadOnlyDictionary<string, string> CustomSettings { get; }

    /// <summary>
    /// Gets the API rate limit per minute.
    /// </summary>
    int? ApiRateLimitPerMinute { get; }

    /// <summary>
    /// Gets whether the tenant has a specific feature enabled.
    /// </summary>
    bool HasFeature(string featureName);
}
