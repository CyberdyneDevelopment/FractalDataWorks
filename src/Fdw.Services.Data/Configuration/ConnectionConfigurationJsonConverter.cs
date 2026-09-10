using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// STJ JsonConverter for <see cref="IConnectionImplementationConfiguration"/> that dispatches to the correct
/// concrete derived type using the <c>Implementation</c> discriminator field.
/// </summary>
/// <remarks>
/// Why: <see cref="IConnectionImplementationConfiguration"/> is the base type in <c>Services.Connections</c>,
/// but derived types (<c>MsSqlConnectionConfiguration</c>, etc.) live in separate packages that
/// <c>Services.Connections</c> cannot reference. <c>[JsonPolymorphic]</c> attributes on the base
/// would create circular package dependencies. Instead, this converter reads
/// <c>Implementation</c> at the start of each object, resolves the concrete <see cref="Type"/>
/// from <see cref="ConnectionTypes"/> (populated by module initializers at assembly load time),
/// and delegates deserialization to the resolved type — zero hardcoded type names.
/// </remarks>
public sealed class ConnectionConfigurationJsonConverter : JsonConverter<IConnectionImplementationConfiguration>
{
    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public string? Environment { get; set; }

    /// <inheritdoc/>
    public bool HealthCheckEnabled { get; set; }

    /// <inheritdoc/>
    public bool HealthCheckOnStartup { get; set; }

    /// <inheritdoc/>
    public int? HealthCheckIntervalSeconds { get; set; }

    /// <inheritdoc/>
    public bool DiscoveryEnabled { get; set; } = true;

    private const string DiscriminatorPropertyName = "Implementation";
    private const string SettingsPropertyName = "Configuration";

    private JsonSerializerOptions? _innerOptions;

    private JsonSerializerOptions GetInnerOptions(JsonSerializerOptions outerOptions)
    {
        if (_innerOptions is not null)
            return _innerOptions;

        var inner = new JsonSerializerOptions(outerOptions);
        inner.Converters.Remove(this);
        _innerOptions = inner;
        return inner;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(IConnectionImplementationConfiguration);

    /// <inheritdoc />
    public override IConnectionImplementationConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var innerOptions = GetInnerOptions(options);

        // The entry names the connection and says which implementation it is; its nested
        // Configuration object is that implementation, and is what this returns. Name and
        // Implementation are stamped onto it from the entry, as the domain provider stamps them
        // from a domain row.
        if (ReadString(root, "Name") is not { Length: > 0 } name)
            throw new JsonException("A connection entry declares no Name.");

        if (ReadString(root, "Implementation") is not { Length: > 0 } implementation)
            throw new JsonException($"Connection '{name}' names no implementation.");

        var connectionType = ConnectionTypes.ByName(implementation);
        if (ReferenceEquals(connectionType, ConnectionTypes.NotFound))
        {
            throw new JsonException(
                $"Connection '{name}' names implementation '{implementation}', "
                + "which is not registered in ConnectionTypes. Reference the package that provides that "
                + "[Implementation] so its module initializer registers it before configuration is loaded.");
        }

        var settingsType = connectionType.ConfigurationType;
        if (settingsType is null || !typeof(IConnectionImplementationConfiguration).IsAssignableFrom(settingsType))
        {
            throw new JsonException(
                $"Connection '{name}' resolved implementation '{implementation}' "
                + $"to configuration type '{settingsType?.FullName ?? "(null)"}', which does not implement "
                + $"{nameof(IConnectionImplementationConfiguration)}.");
        }

        if (FindProperty(root, SettingsPropertyName) is not { ValueKind: JsonValueKind.Object } settingsElement)
            throw new JsonException($"Connection '{name}' has no {SettingsPropertyName} object.");

        if (JsonSerializer.Deserialize(settingsElement.GetRawText(), settingsType, innerOptions)
            is not IConnectionImplementationConfiguration connection)
            return null;

        connection.Name = name;
        connection.Implementation = implementation;
        return connection;
    }

    // Case-insensitive, matching the schema loader's own PropertyNameCaseInsensitive.
    private static JsonElement? FindProperty(JsonElement root, string propertyName)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        return null;
    }

    private static string? ReadString(JsonElement root, string propertyName)
        => FindProperty(root, propertyName) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IConnectionImplementationConfiguration value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), GetInnerOptions(options));
    }
}
