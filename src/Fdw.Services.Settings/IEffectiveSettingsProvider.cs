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

    /// <summary>
    /// Gets the effective value for a setting, telling a setting that is not there from one whose value is the type's default.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="settingName">The setting name to resolve.</param>
    /// <param name="value">The resolved and optionally clamped value; the type's default when the method returns <see langword="false"/>.</param>
    /// <param name="tenantId">Optional tenant ID for tenant-level override lookup.</param>
    /// <param name="roleName">Optional role name for role-level override lookup.</param>
    /// <returns>
    /// <see langword="false"/> when no active server setting has that name or its value does not convert to <typeparamref name="T"/>;
    /// otherwise <see langword="true"/>.
    /// </returns>
    /// <remarks>
    /// Why this exists beside <see cref="GetEffectiveValue{T}"/>: that method answers <c>default</c> for a setting that is absent, so a
    /// setting whose real value is 0 and a setting nobody declared read the same. A setting where 0 means "no limit" must not
    /// turn an absent row into no limit, so it reads through this method and refuses when it returns <see langword="false"/>.
    /// </remarks>
    bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null);
}
