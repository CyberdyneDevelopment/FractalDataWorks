using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>Text that reads as a number.</summary>
[TypeOption(typeof(FilterNumberKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextFilterNumberKind : FilterNumberKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextFilterNumberKind"/> class.</summary>
    public TextFilterNumberKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is string text && decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out _);

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => decimal.Parse((string)value, NumberStyles.Any, CultureInfo.InvariantCulture);
}
