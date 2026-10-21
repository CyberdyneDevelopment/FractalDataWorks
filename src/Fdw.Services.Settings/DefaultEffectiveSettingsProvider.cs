using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>
/// Default implementation of <see cref="IEffectiveSettingsProvider"/> that resolves settings
/// through the layered hierarchy: Server -> Tenant -> Role, with numeric clamping.
/// </summary>
public sealed class DefaultEffectiveSettingsProvider : IEffectiveSettingsProvider
{
    private readonly IServerSettingConfigurationProvider _server;
    private readonly ITenantSettingConfigurationProvider _tenant;
    private readonly IRoleSettingConfigurationProvider _role;
    private readonly ILogger<DefaultEffectiveSettingsProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultEffectiveSettingsProvider"/> class.
    /// </summary>
    public DefaultEffectiveSettingsProvider(
        IServerSettingConfigurationProvider server,
        ITenantSettingConfigurationProvider tenant,
        IRoleSettingConfigurationProvider role,
        ILogger<DefaultEffectiveSettingsProvider>? logger)
    {
        _server = server;
        _tenant = tenant;
        _role = role;
        _logger = logger ?? NullLogger<DefaultEffectiveSettingsProvider>.Instance;
    }

    /// <inheritdoc/>
    public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null)
    {
        var (settingValue, serverSetting) = ResolveSettingValue(settingName, tenantId, roleName);

        if (serverSetting is null)
        {
            return default!;
        }

        var clampedValue = ClampIfNumeric(settingName, settingValue, serverSetting);
        return ConvertValue<T>(settingName, clampedValue, serverSetting.DataType);
    }

    /// <inheritdoc/>
    public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
    {
        value = default!;
        var (settingValue, serverSetting) = ResolveSettingValue(settingName, tenantId, roleName);
        if (serverSetting is null)
        {
            return false;
        }

        return TryConvertValue(settingName, ClampIfNumeric(settingName, settingValue, serverSetting), out value);
    }

#pragma warning disable VSTHRD002 // Synchronously waiting on tasks
#pragma warning disable FDW007 // Why: Sequential layered resolution (server -> tenant -> role) is inherently branchy but straightforward
    private (string Value, IServerSettingImplementationConfiguration? ServerSetting) ResolveSettingValue(
        string settingName,
        Guid? tenantId,
        string? roleName)
    {
        var serverSettingResult = _server.Get(settingName).GetAwaiter().GetResult();
        var serverSetting = serverSettingResult.IsSuccess ? serverSettingResult.Value : null;

        if (serverSetting is null || !serverSetting.IsActive)
        {
            SettingsLog.ServerSettingNotFound(_logger, settingName);
            return (string.Empty, null);
        }

        var effectiveValue = serverSetting.SettingValue;
        SettingsLog.SettingResolvedAtServerLevel(_logger, settingName, effectiveValue);

        // Why: a lock held by a level binds every level below it (FDW-1579), so a locked server value is the value for every tenant and role.
        if (tenantId.HasValue && !serverSetting.IsLocked)
        {
            var tenantSettingsResult = _tenant.Get().GetAwaiter().GetResult();
            var tenantSettings = tenantSettingsResult.IsSuccess ? tenantSettingsResult.Value! : (IReadOnlyList<ITenantSettingImplementationConfiguration>)[];

            var tenantLocked = false;
            foreach (var ts in tenantSettings)
            {
                if (ts.TenantId == tenantId.Value
                    && string.Equals(ts.SettingName, settingName, StringComparison.OrdinalIgnoreCase)
                    && ts.IsActive)
                {
                    effectiveValue = ts.SettingValue;
                    tenantLocked = ts.IsLocked;
                    SettingsLog.SettingOverriddenAtTenantLevel(_logger, settingName, tenantId.Value.ToString());
                    break;
                }
            }

            if (roleName is not null && !tenantLocked)
            {
                var roleSettingsResult = _role.Get().GetAwaiter().GetResult();
                var roleSettings = roleSettingsResult.IsSuccess ? roleSettingsResult.Value! : (IReadOnlyList<IRoleSettingImplementationConfiguration>)[];

                foreach (var rs in roleSettings)
                {
                    if (rs.TenantId == tenantId.Value
                        && string.Equals(rs.RoleName, roleName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(rs.SettingName, settingName, StringComparison.OrdinalIgnoreCase)
                        && rs.IsActive)
                    {
                        effectiveValue = rs.SettingValue;
                        SettingsLog.SettingOverriddenAtRoleLevel(_logger, settingName, roleName, tenantId.Value.ToString());
                        break;
                    }
                }
            }
        }

        return (effectiveValue, serverSetting);
    }
#pragma warning restore VSTHRD002
#pragma warning restore FDW007

    private string ClampIfNumeric(
        string settingName,
        string value,
        IServerSettingImplementationConfiguration serverSetting)
    {
        if (serverSetting.MinValue is null && serverSetting.MaxValue is null)
        {
            return value;
        }

        if (!IsNumericDataType(serverSetting.DataType))
        {
            return value;
        }

        if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
        {
            return value;
        }

        var clamped = numericValue;

        if (serverSetting.MinValue is not null
            && decimal.TryParse(serverSetting.MinValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var min)
            && clamped < min)
        {
            clamped = min;
            var clampedStr = clamped.ToString(CultureInfo.InvariantCulture);
            SettingsLog.SettingClampedToMin(_logger, settingName, value, clampedStr, serverSetting.MinValue);
        }

        if (serverSetting.MaxValue is not null
            && decimal.TryParse(serverSetting.MaxValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var max)
            && clamped > max)
        {
            clamped = max;
            var clampedStr = clamped.ToString(CultureInfo.InvariantCulture);
            SettingsLog.SettingClampedToMax(_logger, settingName, value, clampedStr, serverSetting.MaxValue);
        }

        if (clamped != numericValue)
        {
            return clamped.ToString(CultureInfo.InvariantCulture);
        }

        return value;
    }

    private T ConvertValue<T>(string settingName, string value, string dataType)
        => TryConvertValue<T>(settingName, value, out var converted) ? converted : default!;

    private bool TryConvertValue<T>(string settingName, string value, out T result)
    {
        result = default!;
        try
        {
            var targetType = typeof(T);
            var nullableUnderlying = Nullable.GetUnderlyingType(targetType);
            var underlyingType = nullableUnderlying is not null ? nullableUnderlying : targetType;

            if (underlyingType == typeof(string))
            {
                result = (T)(object)value;
                return true;
            }

            var converter = TypeDescriptor.GetConverter(underlyingType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                var converted = converter.ConvertFromInvariantString(value);
                if (converted is null)
                {
                    return false;
                }

                result = (T)converted;
                return true;
            }

            SettingsLog.SettingConversionFailed(_logger, settingName, value, targetType.Name);
            return false;
        }
        catch (Exception ex)
        {
            SettingsLog.SettingConversionException(_logger, ex, settingName, value, typeof(T).Name, ex.Message);
            return false;
        }
    }

    private static bool IsNumericDataType(string dataType)
    {
        return string.Equals(dataType, "Int32", StringComparison.OrdinalIgnoreCase)
               || string.Equals(dataType, "Int64", StringComparison.OrdinalIgnoreCase)
               || string.Equals(dataType, "Decimal", StringComparison.OrdinalIgnoreCase)
               || string.Equals(dataType, "Double", StringComparison.OrdinalIgnoreCase)
               || string.Equals(dataType, "Single", StringComparison.OrdinalIgnoreCase)
               || string.Equals(dataType, "Float", StringComparison.OrdinalIgnoreCase);
    }
}
