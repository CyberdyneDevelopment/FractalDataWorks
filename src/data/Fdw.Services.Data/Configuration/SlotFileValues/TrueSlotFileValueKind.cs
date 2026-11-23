using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Configuration;

/// <summary>The JSON word true: stated as "true".</summary>
[TypeOption(typeof(SlotFileValueKinds), "True")]
[ExcludeFromCodeCoverage]
public sealed class TrueSlotFileValueKind : SlotFileValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TrueSlotFileValueKind"/> class.</summary>
    public TrueSlotFileValueKind()
        : base("True", JsonValueKind.True)
    {
    }

    /// <inheritdoc />
    public override string? Flatten(JsonElement element, string name, Dictionary<string, string> values) => Add(values, name, "true");
}
