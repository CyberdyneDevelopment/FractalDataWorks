using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// The smallest decimal(precision, scale) that holds a set of values exactly.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct DecimalFacets(byte Precision, byte Scale)
{
    /// <summary>SQL Server's decimal with nothing stated: decimal(18,0).</summary>
    public static readonly DecimalFacets Unstated = new(18, 0);

    /// <summary>Measures the values.</summary>
    /// <param name="values">The values, none NULL.</param>
    /// <returns>The facets; <see cref="Unstated"/> when there are no values to measure.</returns>
    public static DecimalFacets Of(IReadOnlyList<object> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
            return Unstated;

        var whole = 1;
        var scale = 0;
        foreach (var value in values)
        {
            var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            scale = Math.Max(scale, number.Scale);
            var integral = decimal.Truncate(Math.Abs(number));
            whole = Math.Max(whole, integral == 0m ? 1 : integral.ToString(CultureInfo.InvariantCulture).Length);
        }

        return new DecimalFacets((byte)Math.Min(whole + scale, byte.MaxValue), (byte)scale);
    }
}
