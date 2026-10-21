using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Services.Settings;

namespace Fdw.Services.RateLimiting.Tests;

/// <summary>
/// Settings held in memory, as text, the way the platform's settings hold them: a setting that was never set is absent, a value
/// that is not of the kind asked for does not convert, a company's own value wins over the server's, and a locked server value
/// binds every company (as <see cref="DefaultEffectiveSettingsProvider"/> resolves them).
/// </summary>
internal sealed class FakeEffectiveSettings : IEffectiveSettingsProvider
{
    private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _companyValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _locked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Sets a setting to a value for the server.</summary>
    /// <param name="name">The setting's name.</param>
    /// <param name="value">The value, as text.</param>
    /// <returns>These settings.</returns>
    internal FakeEffectiveSettings Set(string name, string value)
    {
        _values[name] = value;
        return this;
    }

    /// <summary>Sets a setting to a value for one company.</summary>
    /// <param name="tenantId">The company.</param>
    /// <param name="name">The setting's name.</param>
    /// <param name="value">The value, as text.</param>
    /// <returns>These settings.</returns>
    internal FakeEffectiveSettings SetForCompany(Guid tenantId, string name, string value)
    {
        _companyValues[CompanyKey(tenantId, name)] = value;
        return this;
    }

    /// <summary>Takes a company's own value for a setting away, as if it had never set one.</summary>
    /// <param name="tenantId">The company.</param>
    /// <param name="name">The setting's name.</param>
    /// <returns>These settings.</returns>
    internal FakeEffectiveSettings ClearForCompany(Guid tenantId, string name)
    {
        _companyValues.Remove(CompanyKey(tenantId, name));
        return this;
    }

    /// <summary>Locks the server's value of a setting, so no company's value for it holds.</summary>
    /// <param name="name">The setting's name.</param>
    /// <returns>These settings.</returns>
    internal FakeEffectiveSettings Lock(string name)
    {
        _locked.Add(name);
        return this;
    }

    /// <summary>Takes a setting away, as if no row held it.</summary>
    /// <param name="name">The setting's name.</param>
    /// <returns>These settings.</returns>
    internal FakeEffectiveSettings Remove(string name)
    {
        _values.Remove(name);
        return this;
    }

    /// <inheritdoc />
    public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null)
        => TryGetEffectiveValue<T>(settingName, out var value, tenantId, roleName) ? value : default!;

    /// <inheritdoc />
    public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
    {
        value = default!;
        if (!_values.TryGetValue(settingName, out var text))
        {
            return false;
        }

        if (tenantId.HasValue
            && !_locked.Contains(settingName)
            && _companyValues.TryGetValue(CompanyKey(tenantId.Value, settingName), out var own))
        {
            text = own;
        }

        try
        {
            value = (T)Convert.ChangeType(text, typeof(T), CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            return false;
        }
    }

    private static string CompanyKey(Guid tenantId, string name)
        => tenantId.ToString("N", CultureInfo.InvariantCulture) + "/" + name;
}
