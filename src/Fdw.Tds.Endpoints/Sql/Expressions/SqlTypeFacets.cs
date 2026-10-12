using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Protocols.Tds.Types;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>
/// The facets a T-SQL type reference states, read the way each kind of type reads them.
/// </summary>
/// <remarks>
/// One parameter list means different things to different types: nvarchar(50) is a length, decimal(10) a
/// precision with scale 0, time(3) a fractional-second scale. The facets carry each reading, and a type
/// takes the one that is its own.
/// </remarks>
public sealed class SqlTypeFacets
{
    private SqlTypeFacets(int length, byte precision, byte scale, byte timeScale, bool stated)
    {
        Length = length;
        Precision = precision;
        Scale = scale;
        TimeScale = timeScale;
        Stated = stated;
    }

    /// <summary>Gets the length of a sized type: characters or bytes, or <see cref="TdsColumnType.Max"/>.</summary>
    public int Length { get; }

    /// <summary>Gets the precision of a decimal.</summary>
    public byte Precision { get; }

    /// <summary>Gets the scale of a decimal: the second number, or 0 when only a precision is written.</summary>
    public byte Scale { get; }

    /// <summary>Gets the fractional-second scale of a time type.</summary>
    public byte TimeScale { get; }

    /// <summary>Gets a value indicating whether any facet was written.</summary>
    public bool Stated { get; }

    /// <summary>Facets a type was declared with in full, such as money's (19,4) or sysname's 128.</summary>
    /// <param name="length">The length.</param>
    /// <param name="precision">The precision.</param>
    /// <param name="scale">The scale.</param>
    /// <returns>The facets.</returns>
    public static SqlTypeFacets Fixed(int length, byte precision, byte scale) => new(length, precision, scale, 7, stated: true);

    /// <summary>Reads the facets a reference's parameters state.</summary>
    /// <param name="parameters">The parameters as parsed.</param>
    /// <param name="defaultLength">The length SQL Server gives a sized type with none written in this context.</param>
    /// <param name="facets">The facets.</param>
    /// <returns>False when a parameter is not a whole number or max.</returns>
    public static bool TryRead(IList<Literal> parameters, int defaultLength, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SqlTypeFacets? facets)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        facets = null;
        if (parameters.Count == 0)
        {
            facets = new SqlTypeFacets(defaultLength, 0, 0, 7, stated: false);
            return true;
        }

        if (!TryNumber(parameters[0], out var first))
            return false;

        var second = 0;
        if (parameters.Count > 1 && !TryNumber(parameters[1], out second))
            return false;

        facets = new SqlTypeFacets(first, Clamp(first, 38), Clamp(second, 38), Clamp(first, 7), stated: true);
        return true;
    }

    private static byte Clamp(int value, int max) => (byte)Math.Clamp(value, 0, max);

    private static bool TryNumber(Literal literal, out int number)
    {
        if (literal is MaxLiteral)
        {
            number = TdsColumnType.Max;
            return true;
        }

        return int.TryParse(literal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
    }
}
