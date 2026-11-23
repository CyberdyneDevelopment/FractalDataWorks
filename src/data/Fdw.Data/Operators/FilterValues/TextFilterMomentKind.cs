using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>Text that reads as a date and time.</summary>
[TypeOption(typeof(FilterMomentKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextFilterMomentKind : FilterMomentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextFilterMomentKind"/> class.</summary>
    public TextFilterMomentKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is string text && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    /// <inheritdoc />
    public override DateTimeOffset ToMoment(object value) => DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.None);
}
