using System;

namespace Fdw.Services.Settings;

/// <summary>
/// Provides effective setting values resolved through the layered settings hierarchy:
/// Server (default) → Tenant (override) → Role (override).
/// </summary>
/// <remarks>
/// Numeric values are clamped to MinValue/MaxValue defined on the server setting when applicable.
/// Settings are resolved from in-memory IOptionsMonitor snapshots, so no async I/O is required.
/// </remarks>
public interface IEffectiveSettingsProvider
{
    /// <summary>
    /// Gets the effective value for a setting, resolved through the layered hierarchy.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="settingName">The setting name to resolve.</param>
    /// <param name="tenantId">Optional tenant ID for tenant-level override lookup.</param>
    /// <param name="roleName">Optional role name for role-level override lookup.</param>
    /// <returns>The resolved and optionally clamped value.</returns>
    T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null);
}
