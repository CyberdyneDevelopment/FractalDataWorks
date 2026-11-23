using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Any other value (an enum, a character, a date, a time of day, a length of time, an unsigned or signed byte number): written
/// as the text it reports for itself, in single quotes, as every OData operator wrote it before the kinds existed. The type of a
/// filter value is the caller's, so the set of types is open; whether a type should have a literal of its own is a product
/// decision (FDW-6888), made against the service that reads it.
/// </summary>
[TypeOption(typeof(ODataLiteralKinds), "Other")]
[ExcludeFromCodeCoverage]
public sealed class OtherODataLiteralKind : ODataLiteralKindBase
{
    /// <summary>Initializes a new instance of the <see cref="OtherODataLiteralKind"/> class.</summary>
    public OtherODataLiteralKind()
        : base("Other")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) =>
        value is not (string or int or long or short or byte or decimal or double or float or bool or DateTime or DateTimeOffset or Guid);

    /// <inheritdoc />
    public override string Literal(object value) => $"'{value}'";
}
