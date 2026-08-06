using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Configuration;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// STJ JsonConverter for <see cref="AegisCommandConfiguration"/> that dispatches to the correct
/// concrete typed body using the <c>ServiceOptionType</c> discriminator field.
/// </summary>
/// <remarks>
/// Mirrors <see cref="ConnectionConfigurationJsonConverter"/> exactly: <see cref="AegisCommandConfiguration"/>
/// is the parent-header type in <c>Fdw.Aegis.Configuration</c>, but typed bodies
/// (<c>PreApprovedCommandConfiguration</c>, <c>AdHocCommandConfiguration</c>) are resolved by reading
/// <c>ServiceOptionType</c> at the start of each object and looking up the resolved <see cref="Type"/>
/// from <see cref="ApprovalPolicyTypes"/> (populated by module initializers at assembly load time) —
/// zero hardcoded type names.
/// </remarks>
public sealed class AegisCommandConfigurationJsonConverter : JsonConverter<AegisCommandConfiguration>
{
    private const string DiscriminatorPropertyName = "ServiceOptionType";
    private const string SettingsPropertyName = "Configuration";

    // Why: CA1869 — inner options must be cached, not created per call. The inner options are built
    // once after the parent options are fully constructed (on first Read/Write call) and exclude
    // this converter to prevent infinite recursion. Lazy<T> ensures thread-safe one-time
    // initialization without locking overhead on the hot path.
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
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(AegisCommandConfiguration);

    /// <inheritdoc />
    public override AegisCommandConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var innerOptions = GetInnerOptions(options);

        // Build the parent-only JSON (everything EXCEPT the typed Configuration child).
        // Why: STJ refuses to deserialize the IApprovalPolicyConfiguration interface property in one
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

        var command = JsonSerializer.Deserialize<AegisCommandConfiguration>(parentJson, innerOptions);
        if (command is null) return null;

        // Resolve typed settings type via ServiceOptionType, deserialize the nested Configuration.
        if (!string.IsNullOrEmpty(command.ServiceOptionType)
            && root.TryGetProperty(SettingsPropertyName, out var settingsElement)
            && settingsElement.ValueKind == JsonValueKind.Object)
        {
            // Why: the JSON declares a Configuration body, so failing to bind it is never benign —
            // silently leaving Configuration null defers the failure to policy evaluation, where it
            // surfaces as an unrelated "could not resolve" far from the real cause.
            var policyType = ApprovalPolicyTypes.ByName(command.ServiceOptionType);
            if (ReferenceEquals(policyType, ApprovalPolicyTypes.NotFound))
            {
                throw new JsonException(
                    $"Command '{command.Name}' declares ServiceOptionType '{command.ServiceOptionType}', "
                    + "which is not registered in ApprovalPolicyTypes. Reference the package that provides that "
                    + "[TypeOption] so its module initializer registers it before configuration is loaded.");
            }

            var settingsType = policyType.ConfigurationType;
            if (settingsType is null || !typeof(IApprovalPolicyConfiguration).IsAssignableFrom(settingsType))
            {
                throw new JsonException(
                    $"Command '{command.Name}' resolved ServiceOptionType '{command.ServiceOptionType}' to "
                    + $"configuration type '{settingsType?.FullName ?? "(null)"}', which does not implement "
                    + $"{nameof(IApprovalPolicyConfiguration)}.");
            }

            command.Configuration = (IApprovalPolicyConfiguration?)JsonSerializer.Deserialize(
                settingsElement.GetRawText(), settingsType, innerOptions);
        }

        return command;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, AegisCommandConfiguration value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), GetInnerOptions(options));
    }
}
