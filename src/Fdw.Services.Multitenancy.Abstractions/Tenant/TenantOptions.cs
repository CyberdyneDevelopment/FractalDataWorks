using System;
using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Default implementation of tenant options.
/// </summary>
public sealed class TenantOptions : ITenantOptions
{
    private readonly HashSet<string> _enabledFeatures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _customSettings = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public int? MaxUsers { get; set; }

    /// <inheritdoc />
    public long? StorageQuotaBytes { get; set; }

    /// <inheritdoc />
    public IEnumerable<string> EnabledFeatures => _enabledFeatures;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> CustomSettings => _customSettings;

    /// <inheritdoc />
    public int? ApiRateLimitPerMinute { get; set; }

    /// <inheritdoc />
    public bool HasFeature(string featureName) => _enabledFeatures.Contains(featureName);

    /// <summary>
    /// Adds an enabled feature.
    /// </summary>
    public void AddFeature(string featureName) => _enabledFeatures.Add(featureName);

    /// <summary>
    /// Sets a custom setting.
    /// </summary>
    public void SetSetting(string key, string value) => _customSettings[key] = value;

    /// <summary>
    /// Gets the default options.
    /// </summary>
    public static TenantOptions Default { get; } = new();
}
