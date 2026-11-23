using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>A number: a long when it is whole and fits one, else a decimal, else out of range.</summary>
[TypeOption(typeof(JsonFilterScalarKinds), "Number")]
[ExcludeFromCodeCoverage]
public sealed class NumberJsonFilterScalarKind : JsonFilterScalarKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NumberJsonFilterScalarKind"/> class.</summary>
    public NumberJsonFilterScalarKind()
        : base("Number", JsonValueKind.Number)
    {
    }

    /// <inheritdoc />
    public override object? Read(JsonElement value)
    {
        if (value.TryGetInt64(out var whole))
        {
            return whole;
        }

        return value.TryGetDecimal(out var fractional) ? fractional : null;
    }
}
