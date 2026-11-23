using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A date and time.</summary>
[TypeOption(typeof(XlsxMoments), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeXlsxMoment : XlsxMomentBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeXlsxMoment"/> class.</summary>
    public DateTimeXlsxMoment()
        : base("DateTime", 2, true)
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTime;

    /// <inheritdoc />
    public override DateTime When(object value) => (DateTime)value;

    /// <inheritdoc />
    public override string IsoText(object value) => ((DateTime)value).ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
}
