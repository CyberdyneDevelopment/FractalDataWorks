using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Abstractions;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Services.Settings.Logging;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>
/// Fills each value a create request's configuration body leaves out (FDW-3580, FDW-3823): the service type's tuning default (the host's
/// registration override, else the value the type or its domain declares, <see cref="ServiceTypeDefaults"/>), else, for a property that still
/// names one with <see cref="DefaultSettingAttribute"/>, the instance setting read for the caller's tenant. A value the caller chooses has
/// neither: its validator refuses the request.
/// </summary>
/// <remarks>
/// Why on the body and before binding: an omitted member is only visible there. Once the body is bound, an unstated
/// number or flag is a 0 or a false, indistinguishable from one the caller sent. Why a setting: Mike, "make it
/// configurable ... as long as it is in configuration and not hardcoded". A value that is neither stated nor set refuses
/// the create, naming the field and the setting.
/// </remarks>
public static class ConfigurationCreateDefaults
{
    /// <summary>Fills the omitted defaultable members of <paramref name="body"/> in place.</summary>
    /// <param name="body">The create request's configuration body.</param>
    /// <param name="configurationType">The configuration type the body binds to; its properties carry the attributes.</param>
    /// <param name="option">The service type being created: its host override or declared tuning default
    /// (<see cref="ServiceTypeDefaults"/>) fills an omitted member first; <see langword="null"/> when no type declares any.</param>
    /// <param name="isStated">Whether the body states a property, by the property's name (a caller with aliases says so here).</param>
    /// <param name="settings">The settings that hold the defaults.</param>
    /// <param name="tenantId">The caller's tenant, whose settings apply.</param>
    /// <param name="ownerKind">What is being created, as a person says it ("connection", "secret manager").</param>
    /// <param name="ownerName">The name of what is being created, for the messages.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The body with every defaultable member stated, or the first refusal.</returns>
    public static IGenericResult<JsonObject> Fill(
        JsonObject body,
        Type configurationType,
        IServiceType? option,
        Func<string, bool> isStated,
        Lazy<IEffectiveSettingsProvider> settings,
        Guid? tenantId,
        string ownerKind,
        string ownerName,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(configurationType);
        ArgumentNullException.ThrowIfNull(isStated);
        ArgumentNullException.ThrowIfNull(logger);

        foreach (var property in configurationType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (isStated(property.Name))
                continue;

            // A choice only the caller makes has no default: refuse the body that leaves it out (FDW-3823).
            if (property.GetCustomAttribute<CallerChoiceAttribute>() is not null)
                return GenericResult<JsonObject>.Failure(InstanceDefaultLog.ChoiceNotStated(logger, ownerKind, ownerName, property.Name));

            // The type carries its tuning defaults (FDW-3823, Mike: "a default value set in the service type option or collection so
            // that in the registration we can change it"): the host's override, else the option's declared value.
            if (option is not null && ServiceTypeDefaults.TryResolve(option, property.Name, out var carried) && carried is not null)
            {
                body[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] = JsonSerializer.SerializeToNode(carried);
                InstanceDefaultLog.TakenFromType(logger, ownerKind, ownerName, property.Name, option.Name);
                continue;
            }

            if (property.GetCustomAttribute<DefaultSettingAttribute>() is not { } declared)
                continue;

            var value = Read(property, declared.Setting, option?.Name, settings, tenantId, ownerKind, ownerName, logger);
            if (value.IsFailure)
                return value.ToNewResult<JsonObject>();

            body[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] = value.Value;
            InstanceDefaultLog.Taken(logger, ownerKind, ownerName, property.Name, declared.Setting);
        }

        return GenericResult<JsonObject>.Success(body);
    }

    /// <summary>Whether <paramref name="body"/> states <paramref name="property"/>: it carries the member, by any casing, with a value.</summary>
    /// <param name="body">The create request's configuration body.</param>
    /// <param name="property">The configuration property's name.</param>
    /// <returns><see langword="true"/> when the body states the property.</returns>
    public static bool IsStated(JsonObject body, string property)
    {
        ArgumentNullException.ThrowIfNull(body);
        return body.Any(member => member.Value is not null && string.Equals(member.Key, property, StringComparison.OrdinalIgnoreCase));
    }

    private static IGenericResult<JsonNode?> Read(
        PropertyInfo property, string setting, string? typeName, Lazy<IEffectiveSettingsProvider> settings, Guid? tenantId, string ownerKind, string ownerName, ILogger logger)
    {
        var type = property.PropertyType.WithoutNullable();
        if (type == typeof(string))
        {
            var text = InstanceDefault.ResolveTextForType(null, settings, setting, typeName, tenantId, ownerKind, ownerName, property.Name, logger);
            return text.IsFailure ? text.ToNewResult<JsonNode?>() : GenericResult<JsonNode?>.Success(JsonValue.Create(text.Value));
        }

        if (type == typeof(int))
        {
            var number = InstanceDefault.ResolveForType<int>(null, settings, setting, typeName, tenantId, ownerKind, ownerName, property.Name, logger);
            return number.IsFailure ? number.ToNewResult<JsonNode?>() : GenericResult<JsonNode?>.Success(JsonValue.Create(number.Value));
        }

        if (type == typeof(bool))
        {
            var flag = InstanceDefault.ResolveForType<bool>(null, settings, setting, typeName, tenantId, ownerKind, ownerName, property.Name, logger);
            return flag.IsFailure ? flag.ToNewResult<JsonNode?>() : GenericResult<JsonNode?>.Success(JsonValue.Create(flag.Value));
        }

        return GenericResult<JsonNode?>.Failure(
            InstanceDefaultLog.TypeUnsupported(logger, ownerKind, ownerName, property.Name, setting, type.Name));
    }
}
