using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fdw.Services.Connections;

/// <summary>
/// Reads a connection configuration's string dictionaries (authentication properties, headers) with
/// case-insensitive keys, the way the configurations themselves build them.
/// </summary>
/// <remarks>
/// Why: System.Text.Json builds a plain <see cref="Dictionary{TKey,TValue}"/>, whose keys are
/// case-sensitive. The connection code reads these by name (<c>Username</c>, <c>SecretKeyName</c>), so a
/// body that sent <c>username</c> would bind and then fail at the first lookup.
/// </remarks>
internal sealed class CaseInsensitiveStringDictionaryConverter : JsonConverter<IDictionary<string, string?>>
{
    /// <inheritdoc />
    public override IDictionary<string, string?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var read = JsonSerializer.Deserialize<Dictionary<string, string?>>(ref reader, options);
        var entries = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (read is null)
            return entries;

        foreach (var entry in read)
            entries[entry.Key] = entry.Value;
        return entries;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IDictionary<string, string?> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var entry in value)
            writer.WriteString(entry.Key, entry.Value);
        writer.WriteEndObject();
    }
}
