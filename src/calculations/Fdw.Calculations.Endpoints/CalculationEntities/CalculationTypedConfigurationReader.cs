using System;
using System.Collections;
using System.Reflection;
using System.Text.Json;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Reads a calculation's type-specific configuration from a request body into the configuration class
/// its calculation entity type declares.
/// </summary>
/// <remarks>
/// The implementation is the record a calculation is written as, so create and update both need it.
/// The JSON carries only the type's own settings (for Windowed: <c>TargetField</c>,
/// <c>WindowFunction</c>); the service stamps the shared fields (name, inputs,
/// output) onto it. Same shape as the secret manager create endpoint: resolve the option, deserialize
/// into its <c>ConfigurationType</c>, and let the option validate what it received.
/// </remarks>
public static class CalculationTypedConfigurationReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializes <paramref name="configuration"/> into <paramref name="calculationEntityType"/>'s
    /// configuration class and validates it.
    /// </summary>
    /// <param name="calculationEntityType">The calculation entity type name, e.g. <c>Windowed</c>.</param>
    /// <param name="configuration">The type-specific settings, as a JSON object.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The typed configuration, or a validation failure naming what was wrong.</returns>
    public static IGenericResult<IGenericConfiguration> Read(
        string calculationEntityType, JsonElement configuration, ILogger logger)
    {
        var option = CalculationEntityTypes.ByName(calculationEntityType);
        if (option == CalculationEntityTypes.NotFound)
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.TypedConfigurationUnknownType(logger, calculationEntityType));

        if (configuration.ValueKind != JsonValueKind.Object)
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.TypedConfigurationUnreadable(
                    logger, calculationEntityType, option.ConfigurationType.Name, "configuration must be a JSON object"));

        var declared = option.Settings.Select(setting => setting.Name).ToList();
        var undeclared = configuration.EnumerateObject()
            .Select(member => member.Name)
            .FirstOrDefault(name => !declared.Contains(name, StringComparer.OrdinalIgnoreCase));
        if (undeclared is not null)
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.ConfigurationMemberNotDeclared(
                    logger, calculationEntityType, undeclared, string.Join(", ", declared)));

        var cleared = configuration.EnumerateObject()
            .Where(member => member.Value.ValueKind == JsonValueKind.Null)
            .Select(member => member.Name)
            .FirstOrDefault();
        if (cleared is not null)
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.ConfigurationMemberNull(logger, calculationEntityType, cleared));

        object? parsed;
        try
        {
            parsed = configuration.Deserialize(option.ConfigurationType, JsonOptions);
        }
        catch (JsonException ex)
        {
            // The framework's text names CLR types and byte positions: it goes to the server log, and the caller is told the member.
            CalculationEndpointLog.TypedConfigurationBindFailed(logger, calculationEntityType, ex.Message);
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.TypedConfigurationUnreadable(
                    logger, calculationEntityType, option.ConfigurationType.Name, UnreadableReason(option.ConfigurationType, ex.Path)));
        }

        if (parsed is not IGenericConfiguration typed)
            return GenericResult<IGenericConfiguration>.Failure(
                CalculationEndpointLog.TypedConfigurationUnreadable(
                    logger, calculationEntityType, option.ConfigurationType.Name, "configuration did not deserialize"));

        var valid = option.ValidateConfiguration(typed);
        if (valid.IsFailure)
            return valid.ToNewResult<IGenericConfiguration>();

        return GenericResult<IGenericConfiguration>.Success(typed);
    }

    /// <summary>
    /// Reads the settings an update sends over the configuration the calculation already holds: a member that is sent replaces the stored
    /// one, a member that is not sent keeps its stored value, and the result is read and validated as a whole by <see cref="Read"/>.
    /// </summary>
    /// <param name="calculationEntityType">The calculation entity type name, e.g. <c>Windowed</c>.</param>
    /// <param name="stored">The configuration the calculation holds.</param>
    /// <param name="sent">The settings the update sent, as a JSON object.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>
    /// The merged typed configuration, or a validation failure naming what was wrong. A stored configuration of another kind
    /// than <paramref name="calculationEntityType"/> has nothing to keep, so the sent settings are read whole.
    /// </returns>
    public static IGenericResult<IGenericConfiguration> ReadOver(
        string calculationEntityType, IGenericConfiguration stored, JsonElement sent, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(stored);

        var option = CalculationEntityTypes.ByName(calculationEntityType);
        if (option == CalculationEntityTypes.NotFound
            || sent.ValueKind != JsonValueKind.Object
            || !option.ConfigurationType.IsInstanceOfType(stored))
            return Read(calculationEntityType, sent, logger);

        // A stored member that holds nothing is left out, so it takes the configuration class's default as it would on create.
        var merged = new Dictionary<string, object?>(
            option.SettingValues(stored).Where(pair => pair.Value is not null), StringComparer.OrdinalIgnoreCase);
        foreach (var member in sent.EnumerateObject())
            merged[member.Name] = member.Value;

        return Read(calculationEntityType, JsonSerializer.SerializeToElement(merged), logger);
    }

    // The caller's reason for a configuration the JSON binder refused: the member the path names and the kind of value it takes.
    private static string UnreadableReason(Type configurationType, string? path)
    {
        if (path is null || !path.StartsWith("$.", StringComparison.Ordinal))
            return "the configuration is not valid for this type";

        var member = path[2..];
        var property = configurationType.GetProperty(
            member.Split('.', '[')[0], BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return property is null
            ? $"member '{member}' holds a value of the wrong type"
            : $"member '{member}' holds a value of the wrong type; it takes {ValueKind(property.PropertyType)}";
    }

    // What a person sends for a member of this type, in words; no CLR name is ever written.
    private static string ValueKind(Type memberType)
    {
        var type = Nullable.GetUnderlyingType(memberType) is { } inner ? inner : memberType;
        return type == typeof(int) || type == typeof(long) || type == typeof(short) ? "a whole number"
            : type == typeof(decimal) || type == typeof(double) || type == typeof(float) ? "a number"
            : type == typeof(bool) ? "true or false"
            : type == typeof(string) ? "text"
            : typeof(IEnumerable).IsAssignableFrom(type) ? "a list"
            : "the kind of value that member holds";
    }
}
