using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Configuration;

/// <summary>The base of every slot file value kind.</summary>
[ExcludeFromCodeCoverage]
public abstract class SlotFileValueKindBase : TypeOptionBase<SlotFileValueKindBase>, ISlotFileValueKind
{
    /// <summary>Initializes a new instance of the <see cref="SlotFileValueKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    /// <param name="valueKind">The JSON value kind this reads.</param>
    protected SlotFileValueKindBase(string name, JsonValueKind valueKind)
        : base(name)
    {
        ValueKind = valueKind;
    }

    /// <inheritdoc />
    [TypeLookup("ByValueKind")]
    public JsonValueKind ValueKind { get; }

    /// <inheritdoc />
    public abstract string? Flatten(JsonElement element, string name, Dictionary<string, string> values);

    /// <summary>States a value under a name.</summary>
    /// <param name="values">The named values so far.</param>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    /// <returns>The name when it was already stated; null when the value was added.</returns>
    protected static string? Add(Dictionary<string, string> values, string name, string value) =>
        values.TryAdd(name, value) ? null : name;
}
