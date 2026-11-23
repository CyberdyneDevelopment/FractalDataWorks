using System.Collections.Generic;
using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Services.Data.Configuration;

/// <summary>What a slot file does with a JSON value of one kind when it flattens the file into named values.</summary>
public interface ISlotFileValueKind : ITypeOption<int, SlotFileValueKindBase>
{
    /// <summary>Gets the JSON value kind this reads.</summary>
    JsonValueKind ValueKind { get; }

    /// <summary>Flattens a value of this kind into the named values.</summary>
    /// <param name="element">The JSON value.</param>
    /// <param name="name">The name the value is stated under.</param>
    /// <param name="values">The named values so far.</param>
    /// <returns>The name that was stated twice, or null when none was.</returns>
    string? Flatten(JsonElement element, string name, Dictionary<string, string> values);
}
