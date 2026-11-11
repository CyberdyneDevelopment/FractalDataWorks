using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Json;

/// <summary>
/// Writes a type option in a command result as its name, the way the results wrote the enums these options replaced, instead of as an
/// object of every member the option has.
/// </summary>
/// <typeparam name="TOption">The option interface of the collection.</typeparam>
public abstract class TypeOptionNameJsonConverter<TOption> : JsonConverter<TOption>
    where TOption : class, ITypeOption
{
    /// <inheritdoc />
    public override TOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ByName(reader.GetString() ?? string.Empty);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TOption value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Name);
    }

    /// <summary>The option with this name, or the collection's not-found value.</summary>
    /// <param name="name">The name read.</param>
    /// <returns>The option.</returns>
    protected abstract TOption ByName(string name);
}
