using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// STJ JsonConverter for <see cref="SecretManagerConfiguration"/> that dispatches to the correct
/// concrete derived type using the <c>ServiceOptionType</c> discriminator field.
/// </summary>
/// <remarks>
/// Why: Mirrors <see cref="ConnectionConfigurationJsonConverter"/>. The base type lives in
/// <c>Services.SecretManagers</c>; derived types live in separate packages. Module initializers
/// register each derived type into <see cref="SecretManagerTypes"/> at assembly load time.
/// </remarks>
public sealed class SecretManagerConfigurationJsonConverter : JsonConverter<SecretManagerConfiguration>
{
    private const string DiscriminatorPropertyName = "ServiceOptionType";
    private const string SettingsPropertyName = "Configuration";

    // Why: CA1869 — cached inner options exclude this converter to prevent infinite recursion.
    // Thread-safe via volatile write: the options object is immutable once constructed so
    // a data race produces at most two identical objects, one of which is discarded.
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
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(SecretManagerConfiguration);

    /// <inheritdoc />
    public override SecretManagerConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var innerOptions = GetInnerOptions(options);

        // Strip nested Configuration before first pass — STJ can't deserialize ISecretManagerConfiguration.
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

        var secretManager = JsonSerializer.Deserialize<SecretManagerConfiguration>(parentJson, innerOptions);
        if (secretManager is null) return null;

        if (!string.IsNullOrEmpty(secretManager.ServiceOptionType)
            && root.TryGetProperty(SettingsPropertyName, out var settingsElement)
            && settingsElement.ValueKind == JsonValueKind.Object)
        {
            // Why: the JSON declares a Configuration body, so failing to bind it is never benign —
            // silently leaving Configuration null defers the failure to service construction, where
            // it surfaces as an unrelated "could not resolve" far from the real cause.
            var secretManagerType = SecretManagerTypes.ByName(secretManager.ServiceOptionType);
            if (ReferenceEquals(secretManagerType, SecretManagerTypes.NotFound))
            {
                throw new JsonException(
                    $"SecretManager '{secretManager.Name}' declares ServiceOptionType '{secretManager.ServiceOptionType}', "
                    + "which is not registered in SecretManagerTypes. Reference the package that provides that "
                    + "[ServiceTypeOption] so its module initializer registers it before configuration is loaded.");
            }

            var settingsType = secretManagerType.ConfigurationType;
            if (settingsType is null || !typeof(ISecretManagerConfiguration).IsAssignableFrom(settingsType))
            {
                throw new JsonException(
                    $"SecretManager '{secretManager.Name}' resolved ServiceOptionType '{secretManager.ServiceOptionType}' "
                    + $"to configuration type '{settingsType?.FullName ?? "(null)"}', which does not implement "
                    + $"{nameof(ISecretManagerConfiguration)}.");
            }

            secretManager.Configuration = (ISecretManagerConfiguration?)JsonSerializer.Deserialize(
                settingsElement.GetRawText(), settingsType, innerOptions);
        }

        return secretManager;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, SecretManagerConfiguration value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), GetInnerOptions(options));
    }
}
