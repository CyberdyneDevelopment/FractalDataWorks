using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Configuration;

/// <summary>A number: stated as its raw text.</summary>
[TypeOption(typeof(SlotFileValueKinds), "Number")]
[ExcludeFromCodeCoverage]
public sealed class NumberSlotFileValueKind : SlotFileValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NumberSlotFileValueKind"/> class.</summary>
    public NumberSlotFileValueKind()
        : base("Number", JsonValueKind.Number)
    {
    }

    /// <inheritdoc />
    public override string? Flatten(JsonElement element, string name, Dictionary<string, string> values) => Add(values, name, element.GetRawText());
}
