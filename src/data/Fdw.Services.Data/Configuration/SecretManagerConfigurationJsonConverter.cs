using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Services.SecretManagers;
using Fdw.Services.Data.Logging;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// STJ JsonConverter for <see cref="ISecretManagerImplementationConfiguration"/> that dispatches to the concrete body of the kind the entry names
/// in <c>Implementation</c>, as <see cref="ConnectionConfigurationJsonConverter"/> does for a connection (FDW-6360).
/// </summary>
/// <remarks>
/// The derived types live in the secret manager kinds' own packages, which this one cannot reference, so the converter reads
/// <c>Implementation</c> at the start of each entry and resolves the body type from <see cref="SecretManagerTypes"/>, which the module
/// initializers of the referenced kinds populate. An entry that names no kind, a kind nobody registered, or has no <c>Configuration</c>
/// body is refused naming the entry; nothing is bound to a default kind.
/// </remarks>
public sealed class SecretManagerConfigurationJsonConverter : JsonConverter<ISecretManagerImplementationConfiguration>
{
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
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(ISecretManagerImplementationConfiguration);

    /// <inheritdoc />
    public override ISecretManagerImplementationConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (ReadString(root, "Name") is not { Length: > 0 } name)
            throw new JsonException("A secret manager entry declares no Name.");

        if (ReadString(root, "Implementation") is not { Length: > 0 } implementation)
            throw new JsonException($"Secret manager '{name}' names no implementation.");

        var kind = SecretManagerTypes.ByName(implementation);
        if (ReferenceEquals(kind, SecretManagerTypes.NotFound))
        {
            throw new SchemaRefusalException(
                $"Secret manager '{name}' names implementation '{implementation}', which is not registered in SecretManagerTypes. "
                + "Reference the package that provides that [Implementation] so its module initializer registers it before the schema is loaded.",
                log => ConfigurationGatewayProviderLog.SecretManagerKindNotRegistered(log, name, implementation));
        }

        var bodyType = kind.ConfigurationType;
        if (!typeof(ISecretManagerImplementationConfiguration).IsAssignableFrom(bodyType))
        {
            throw new JsonException(
                $"Secret manager '{name}' resolved implementation '{implementation}' to configuration type '{bodyType.FullName}', "
                + $"which does not implement {nameof(ISecretManagerImplementationConfiguration)}.");
        }

        if (FindProperty(root, SettingsPropertyName) is not { ValueKind: JsonValueKind.Object } body)
            throw new JsonException($"Secret manager '{name}' has no {SettingsPropertyName} object.");

        if (JsonSerializer.Deserialize(body.GetRawText(), bodyType, GetInnerOptions(options)) is not ISecretManagerImplementationConfiguration manager)
            return null;

        manager.Name = name;
        manager.Implementation = implementation;
        return manager;
    }

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
    public override void Write(Utf8JsonWriter writer, ISecretManagerImplementationConfiguration value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), GetInnerOptions(options));
}
