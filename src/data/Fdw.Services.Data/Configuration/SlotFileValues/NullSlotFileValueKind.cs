using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Configuration;

/// <summary>The JSON word null: not stated.</summary>
[TypeOption(typeof(SlotFileValueKinds), "Null")]
[ExcludeFromCodeCoverage]
public sealed class NullSlotFileValueKind : SlotFileValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NullSlotFileValueKind"/> class.</summary>
    public NullSlotFileValueKind()
        : base("Null", JsonValueKind.Null)
    {
    }

    /// <inheritdoc />
    public override string? Flatten(JsonElement element, string name, Dictionary<string, string> values) => null;
}
