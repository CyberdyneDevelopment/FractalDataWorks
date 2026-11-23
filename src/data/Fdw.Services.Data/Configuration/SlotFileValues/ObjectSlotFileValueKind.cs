using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Configuration;

/// <summary>An object: each property is stated under its name, joined to this one.</summary>
[TypeOption(typeof(SlotFileValueKinds), "Object")]
[ExcludeFromCodeCoverage]
public sealed class ObjectSlotFileValueKind : SlotFileValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ObjectSlotFileValueKind"/> class.</summary>
    public ObjectSlotFileValueKind()
        : base("Object", JsonValueKind.Object)
    {
    }

    /// <inheritdoc />
    public override string? Flatten(JsonElement element, string name, Dictionary<string, string> values) => SlotFile.FlattenObject(element, name, values);
}
