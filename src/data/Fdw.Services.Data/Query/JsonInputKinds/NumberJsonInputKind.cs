using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>A JSON number, read as a decimal.</summary>
[TypeOption(typeof(JsonInputKinds), "Number", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NumberJsonInputKind : JsonInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NumberJsonInputKind"/> class.</summary>
    public NumberJsonInputKind()
        : base("Number", JsonValueKind.Number)
    {
    }

    /// <inheritdoc />
    public override object? Read(JsonElement value) => value.TryGetDecimal(out var number) ? number : (decimal?)null;
}
