using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Configuration;

/// <summary>The JSON word false: stated as "false".</summary>
[TypeOption(typeof(SlotFileValueKinds), "False")]
[ExcludeFromCodeCoverage]
public sealed class FalseSlotFileValueKind : SlotFileValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FalseSlotFileValueKind"/> class.</summary>
    public FalseSlotFileValueKind()
        : base("False", JsonValueKind.False)
    {
    }

    /// <inheritdoc />
    public override string? Flatten(JsonElement element, string name, Dictionary<string, string> values) => Add(values, name, "false");
}
