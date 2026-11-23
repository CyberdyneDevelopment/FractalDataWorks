using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Configuration;

/// <summary>The kinds of JSON value a slot file holds, each found by its <see cref="JsonValueKind"/>.</summary>
[TypeCollection(typeof(SlotFileValueKindBase), typeof(ISlotFileValueKind), typeof(SlotFileValueKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class SlotFileValueKinds : TypeCollectionBase<SlotFileValueKindBase, ISlotFileValueKind>
{
    /// <summary>Flattens a JSON value by the kind it is.</summary>
    /// <param name="element">The JSON value.</param>
    /// <param name="name">The name the value is stated under.</param>
    /// <param name="values">The named values so far.</param>
    /// <returns>The name that was stated twice, or null when none was.</returns>
    /// <exception cref="InvalidOperationException">No kind reads the value's JSON value kind.</exception>
    public static string? Flatten(JsonElement element, string name, Dictionary<string, string> values)
    {
        var kind = ByValueKind(element.ValueKind);
        return ReferenceEquals(kind, NotFound)
            ? throw new InvalidOperationException($"A JSON value of kind {element.ValueKind} has no slot file value kind, so '{name}' cannot be read.")
            : kind.Flatten(element, name, values);
    }
}
