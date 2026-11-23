using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>Text that reads as a whole number in the invariant culture.</summary>
[TypeOption(typeof(WholeNumberInputKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextWholeNumberInputKind : WholeNumberInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextWholeNumberInputKind"/> class.</summary>
    public TextWholeNumberInputKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is string text && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    /// <inheritdoc />
    public override long Read(object value) => long.Parse((string)value, NumberStyles.Integer, CultureInfo.InvariantCulture);
}
