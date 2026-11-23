using System;
using Fdw.Collections;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>One kind of date or time a record can hold, and how a worksheet stores it.</summary>
public interface IXlsxMoment : ITypeOption<int, XlsxMomentBase>
{
    /// <summary>Gets the id of the cell style that formats a number of this kind as a date or time.</summary>
    int Style { get; }

    /// <summary>Gets whether the value is a calendar date, so a serial before the first day Excel's calendar has is wrong.</summary>
    bool HasCalendarDate { get; }

    /// <summary>Tells whether a record value is of this kind.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>True when this kind holds the value.</returns>
    bool Holds(object value);

    /// <summary>Gets the value as the point in time a serial number counts from.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The date and time, with a time of day placed on the epoch.</returns>
    DateTime When(object value);

    /// <summary>Writes the value as ISO 8601 text, for a date no serial number can hold.</summary>
    /// <param name="value">A value this kind <see cref="Holds"/>.</param>
    /// <returns>The text.</returns>
    string IsoText(object value);
}
