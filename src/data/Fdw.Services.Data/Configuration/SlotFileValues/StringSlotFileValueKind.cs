using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Configuration;

/// <summary>A string: stated as it is; an empty string is not stated.</summary>
[TypeOption(typeof(SlotFileValueKinds), "String")]
[ExcludeFromCodeCoverage]
public sealed class StringSlotFileValueKind : SlotFileValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="StringSlotFileValueKind"/> class.</summary>
    public StringSlotFileValueKind()
        : base("String", JsonValueKind.String)
    {
    }

    /// <inheritdoc />
    public override string? Flatten(JsonElement element, string name, Dictionary<string, string> values)
        => element.GetString() is { Length: > 0 } text ? Add(values, name, text) : null;
}
