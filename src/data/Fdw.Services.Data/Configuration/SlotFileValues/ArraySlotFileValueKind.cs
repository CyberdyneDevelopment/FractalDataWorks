using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Configuration;

/// <summary>An array: each item is stated under its position, from 0, joined to this name.</summary>
[TypeOption(typeof(SlotFileValueKinds), "Array")]
[ExcludeFromCodeCoverage]
public sealed class ArraySlotFileValueKind : SlotFileValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ArraySlotFileValueKind"/> class.</summary>
    public ArraySlotFileValueKind()
        : base("Array", JsonValueKind.Array)
    {
    }

    /// <inheritdoc />
    public override string? Flatten(JsonElement element, string name, Dictionary<string, string> values) => SlotFile.FlattenArray(element, name, values);
}
