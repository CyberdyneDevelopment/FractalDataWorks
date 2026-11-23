using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A time of day, placed on the workbook's epoch.</summary>
[TypeOption(typeof(XlsxMoments), "Time")]
[ExcludeFromCodeCoverage]
public sealed class TimeOnlyXlsxMoment : XlsxMomentBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeOnlyXlsxMoment"/> class.</summary>
    public TimeOnlyXlsxMoment()
        : base("Time", 4, false)
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is TimeOnly;

    /// <inheritdoc />
    public override DateTime When(object value) => XlsxRowWriter.Epoch.Add(((TimeOnly)value).ToTimeSpan());

    /// <inheritdoc />
    public override string IsoText(object value) => ((TimeOnly)value).ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
}
