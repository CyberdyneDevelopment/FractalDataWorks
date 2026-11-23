using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections;

namespace Fdw.Data;

/// <summary>The base of every OData literal kind.</summary>
[ExcludeFromCodeCoverage]
public abstract class ODataLiteralKindBase : TypeOptionBase<ODataLiteralKindBase>, IODataLiteralKind
{
    /// <summary>Initializes a new instance of the <see cref="ODataLiteralKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    protected ODataLiteralKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract string Literal(object value);

    /// <summary>Writes a number in the invariant culture.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The invariant text.</returns>
    protected static string Invariant(object value) => ((System.IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
}
