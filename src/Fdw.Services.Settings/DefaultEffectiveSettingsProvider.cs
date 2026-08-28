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
    private readonly SettingsConfigurationProvider _provider;
    private readonly ILogger<DefaultEffectiveSettingsProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultEffectiveSettingsProvider"/> class.
    /// </summary>
    public DefaultEffectiveSettingsProvider(
        SettingsConfigurationProvider provider,
        ILogger<DefaultEffectiveSettingsProvider>? logger)
    {
        _provider = provider;
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

#pragma warning disable VSTHRD002 // Synchronously waiting on tasks
#pragma warning disable FDW007 // Why: Sequential layered resolution (server -> tenant -> role) is inherently branchy but straightforward
    private (string Value, ServerSettingConfiguration? ServerSetting) ResolveSettingValue(
        string settingName,
        Guid? tenantId,
        string? roleName)
    {
        var serverSettingResult = _provider.GetServerSetting(settingName).GetAwaiter().GetResult();
        var serverSetting = serverSettingResult.IsSuccess ? serverSettingResult.Value : null;

        if (serverSetting is null || !serverSetting.IsActive)
        {
            SettingsLog.ServerSettingNotFound(_logger, settingName);
            return (string.Empty, null);
        }

        var effectiveValue = serverSetting.SettingValue;
        SettingsLog.SettingResolvedAtServerLevel(_logger, settingName, effectiveValue);

        if (tenantId.HasValue)
        {
            var tenantSettingsResult = _provider.GetTenantSettings().GetAwaiter().GetResult();
            var tenantSettings = tenantSettingsResult.IsSuccess ? tenantSettingsResult.Value! : (IReadOnlyList<TenantSettingConfiguration>)[];

            foreach (var ts in tenantSettings)
            {
                if (ts.TenantId == tenantId.Value
                    && string.Equals(ts.SettingName, settingName, StringComparison.OrdinalIgnoreCase)
                    && ts.IsActive)
                {
                    effectiveValue = ts.SettingValue;
                    SettingsLog.SettingOverriddenAtTenantLevel(_logger, settingName, tenantId.Value.ToString());
                    break;
                }
            }

            if (roleName is not null)
            {
                var roleSettingsResult = _provider.GetRoleSettings().GetAwaiter().GetResult();
                var roleSettings = roleSettingsResult.IsSuccess ? roleSettingsResult.Value! : (IReadOnlyList<RoleSettingConfiguration>)[];

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
        ServerSettingConfiguration serverSetting)
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
    {
        try
        {
            var targetType = typeof(T);
            var nullableUnderlying = Nullable.GetUnderlyingType(targetType);
            var underlyingType = nullableUnderlying is not null ? nullableUnderlying : targetType;

            if (underlyingType == typeof(string))
            {
                return (T)(object)value;
            }

            var converter = TypeDescriptor.GetConverter(underlyingType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                var converted = converter.ConvertFromInvariantString(value);
                return converted is null ? default! : (T)converted;
            }

            SettingsLog.SettingConversionFailed(_logger, settingName, value, targetType.Name);
            return default!;
        }
        catch (Exception ex)
        {
            SettingsLog.SettingConversionException(_logger, ex, settingName, value, typeof(T).Name, ex.Message);
            return default!;
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
