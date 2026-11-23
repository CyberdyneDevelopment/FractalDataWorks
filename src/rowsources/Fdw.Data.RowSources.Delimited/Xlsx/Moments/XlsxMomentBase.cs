using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>The base of every moment kind.</summary>
[ExcludeFromCodeCoverage]
public abstract class XlsxMomentBase : TypeOptionBase<XlsxMomentBase>, IXlsxMoment
{
    /// <summary>Initializes a new instance of the <see cref="XlsxMomentBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    /// <param name="style">The id of the cell style that formats a number of this kind.</param>
    /// <param name="hasCalendarDate">Whether the value is a calendar date.</param>
    protected XlsxMomentBase(string name, int style, bool hasCalendarDate)
        : base(name)
    {
        Style = style;
        HasCalendarDate = hasCalendarDate;
    }

    /// <inheritdoc />
    public int Style { get; }

    /// <inheritdoc />
    public bool HasCalendarDate { get; }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract DateTime When(object value);

    /// <inheritdoc />
    public abstract string IsoText(object value);
}
