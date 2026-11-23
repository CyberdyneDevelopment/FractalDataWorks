using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>The kinds of date and time a record can hold and a worksheet stores as a serial number.</summary>
/// <remarks>A value no kind holds answers the NotFound sentinel; the writer refuses it.</remarks>
[TypeCollection(typeof(XlsxMomentBase), typeof(IXlsxMoment), typeof(XlsxMoments))]
[ExcludeFromCodeCoverage]
public abstract partial class XlsxMoments : TypeCollectionBase<XlsxMomentBase, IXlsxMoment>
{
    /// <summary>Finds the kind that holds a record value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The kind, or <c>NotFound</c> when none holds the value.</returns>
    public static IXlsxMoment OfValue(object value)
    {
        foreach (var moment in All())
        {
            if (moment.Holds(value))
            {
                return moment;
            }
        }

        return NotFound;
    }
}
