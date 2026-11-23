using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A literal that is a number, a decimal or a whole number, stored in the invariant culture.</summary>
[TypeOption(typeof(LiteralTypes), "Number")]
[ExcludeFromCodeCoverage]
public sealed class NumberLiteralType : LiteralTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NumberLiteralType"/> class.</summary>
    public NumberLiteralType()
        : base("Number")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object? value) => value is decimal || value is int;

    /// <inheritdoc />
    public override string Write(object value) => Convert.ToString(value, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override object? Read(string text) => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : (decimal?)null;
}
