using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// STJ JsonConverter for <see cref="ConnectionConfiguration"/> that dispatches to the correct
/// concrete derived type using the <c>ServiceOptionType</c> discriminator field.
/// </summary>
/// <remarks>
/// Why: <see cref="ConnectionConfiguration"/> is the base type in <c>Services.Connections</c>,
/// but derived types (<c>MsSqlConnectionConfiguration</c>, etc.) live in separate packages that
/// <c>Services.Connections</c> cannot reference. <c>[JsonPolymorphic]</c> attributes on the base
/// would create circular package dependencies. Instead, this converter reads
/// <c>ServiceOptionType</c> at the start of each object, resolves the concrete <see cref="Type"/>
/// from <see cref="ConnectionTypes"/> (populated by module initializers at assembly load time),
/// and delegates deserialization to the resolved type — zero hardcoded type names.
/// </remarks>
public sealed class ConnectionConfigurationJsonConverter : JsonConverter<ConnectionConfiguration>
{
    private const string DiscriminatorPropertyName = "ServiceOptionType";
    private const string SettingsPropertyName = "Configuration";

    // Why: CA1869 — inner options must be cached, not created per call. The inner options
    // are built once after the parent options are fully constructed (on first Read/Write call)
    // and exclude this converter to prevent infinite recursion. Lazy<T> ensures thread-safe
    // one-time initialization without locking overhead on the hot path.
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
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(ConnectionConfiguration);

    /// <inheritdoc />
    public override ConnectionConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var innerOptions = GetInnerOptions(options);

        // Build the parent-only JSON (everything EXCEPT the typed Configuration child).
        // Why: STJ refuses to deserialize the IConnectionImplementationConfiguration interface property in one
        // pass — the concrete type isn't known until we resolve it via the ServiceOptionType
        // discriminator. Strip Configuration here and re-attach manually below.
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var prop in root.EnumerateObject())
            {
                if (!string.Equals(prop.Name, SettingsPropertyName, StringComparison.Ordinal))
                    prop.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        var parentJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        var connection = JsonSerializer.Deserialize<ConnectionConfiguration>(parentJson, innerOptions);
        if (connection is null) return null;

        // Resolve typed settings type via ServiceOptionType, deserialize the nested Configuration.
        if (!string.IsNullOrEmpty(connection.ServiceOptionType)
            && root.TryGetProperty(SettingsPropertyName, out var settingsElement)
            && settingsElement.ValueKind == JsonValueKind.Object)
        {
            // Why: the JSON declares a Configuration body, so failing to bind it is never benign —
            // silently leaving Configuration null defers the failure to connection construction,
            // where it surfaces as an unrelated "could not resolve" far from the real cause.
            var connectionType = ConnectionTypes.ByName(connection.ServiceOptionType);
            if (ReferenceEquals(connectionType, ConnectionTypes.NotFound))
            {
                throw new JsonException(
                    $"Connection '{connection.Name}' declares ServiceOptionType '{connection.ServiceOptionType}', "
                    + "which is not registered in ConnectionTypes. Reference the package that provides that "
                    + "[ServiceTypeOption] so its module initializer registers it before configuration is loaded.");
            }

            var settingsType = connectionType.ConfigurationType;
            if (settingsType is null || !typeof(IConnectionImplementationConfiguration).IsAssignableFrom(settingsType))
            {
                throw new JsonException(
                    $"Connection '{connection.Name}' resolved ServiceOptionType '{connection.ServiceOptionType}' "
                    + $"to configuration type '{settingsType?.FullName ?? "(null)"}', which does not implement "
                    + $"{nameof(IConnectionImplementationConfiguration)}.");
            }

            connection.Configuration = (IConnectionImplementationConfiguration?)JsonSerializer.Deserialize(
                settingsElement.GetRawText(), settingsType, innerOptions);
        }

        return connection;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConnectionConfiguration value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), GetInnerOptions(options));
    }
}
