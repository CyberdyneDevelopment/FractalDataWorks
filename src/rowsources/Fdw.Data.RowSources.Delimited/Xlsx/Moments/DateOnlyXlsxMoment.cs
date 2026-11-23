using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A calendar date.</summary>
[TypeOption(typeof(XlsxMoments), "Date")]
[ExcludeFromCodeCoverage]
public sealed class DateOnlyXlsxMoment : XlsxMomentBase
{
    /// <summary>Initializes a new instance of the <see cref="DateOnlyXlsxMoment"/> class.</summary>
    public DateOnlyXlsxMoment()
        : base("Date", 3, true)
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateOnly;

    /// <inheritdoc />
    public override DateTime When(object value) => ((DateOnly)value).ToDateTime(TimeOnly.MinValue);

    /// <inheritdoc />
    public override string IsoText(object value) => ((DateOnly)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
