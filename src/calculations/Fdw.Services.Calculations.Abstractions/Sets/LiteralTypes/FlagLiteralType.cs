using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A literal that is true or false, stored as the word.</summary>
[TypeOption(typeof(LiteralTypes), "Flag")]
[ExcludeFromCodeCoverage]
public sealed class FlagLiteralType : LiteralTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="FlagLiteralType"/> class.</summary>
    public FlagLiteralType()
        : base("Flag")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object? value) => value is bool;

    /// <inheritdoc />
    public override string Write(object value) => (bool)value ? "true" : "false";

    /// <inheritdoc />
    public override object? Read(string text) => bool.TryParse(text, out var flag) ? flag : (bool?)null;
}
