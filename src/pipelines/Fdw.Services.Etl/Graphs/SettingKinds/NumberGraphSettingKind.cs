using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A setting that holds a whole number.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphSettingKinds), "Number")]
public sealed class NumberGraphSettingKind : GraphSettingKindBase
{
    /// <summary>Initializes a new instance of the <see cref="NumberGraphSettingKind"/> class.</summary>
    public NumberGraphSettingKind()
        : base("Number", "Int32", "a whole number")
    {
    }

    /// <inheritdoc />
    public override object? ReadJson(JsonElement value) => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;

    /// <inheritdoc />
    public override bool TryConvert(object value, out object? typed)
    {
        typed = null;
        if (value is not IConvertible number)
        {
            return false;
        }

        try
        {
            typed = number.ToInt32(CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            typed = null;
            return false;
        }
    }
}
