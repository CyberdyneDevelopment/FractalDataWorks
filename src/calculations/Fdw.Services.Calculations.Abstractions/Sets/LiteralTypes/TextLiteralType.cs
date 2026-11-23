using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A literal that is text, stored as it is.</summary>
[TypeOption(typeof(LiteralTypes), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextLiteralType : LiteralTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TextLiteralType"/> class.</summary>
    public TextLiteralType()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object? value) => value is string;

    /// <inheritdoc />
    public override string Write(object value) => (string)value;

    /// <inheritdoc />
    public override object? Read(string text) => text;
}
