using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Configuration;

/// <summary>A value with no JSON kind: not stated.</summary>
[TypeOption(typeof(SlotFileValueKinds), "Undefined")]
[ExcludeFromCodeCoverage]
public sealed class UndefinedSlotFileValueKind : SlotFileValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="UndefinedSlotFileValueKind"/> class.</summary>
    public UndefinedSlotFileValueKind()
        : base("Undefined", JsonValueKind.Undefined)
    {
    }

    /// <inheritdoc />
    public override string? Flatten(JsonElement element, string name, Dictionary<string, string> values) => null;
}
