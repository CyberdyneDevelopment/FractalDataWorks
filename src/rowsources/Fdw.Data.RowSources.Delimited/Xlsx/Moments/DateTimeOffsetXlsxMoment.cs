using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A date and time with an offset, stored as UTC.</summary>
[TypeOption(typeof(XlsxMoments), "DateTimeOffset")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeOffsetXlsxMoment : XlsxMomentBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetXlsxMoment"/> class.</summary>
    public DateTimeOffsetXlsxMoment()
        : base("DateTimeOffset", 2, true)
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override DateTime When(object value) => ((DateTimeOffset)value).UtcDateTime;

    /// <inheritdoc />
    public override string IsoText(object value) => ((DateTimeOffset)value).ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture);
}
