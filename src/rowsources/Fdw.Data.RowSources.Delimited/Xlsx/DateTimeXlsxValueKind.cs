using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A date or a date with a time, read as a date and time.</summary>
[TypeOption(typeof(XlsxValueKinds), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeXlsxValueKind : XlsxValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeXlsxValueKind"/> class.</summary>
    public DateTimeXlsxValueKind() : base("DateTime")
    {
    }

    /// <inheritdoc />
    public override bool CoversBuiltInFormat(int formatId) => formatId is 14 or 15 or 16 or 17 or 22 or (>= 27 and <= 36) or (>= 50 and <= 58);

    /// <inheritdoc />
    public override object Read(string text, bool date1904)
        => XlsxSerials.Moment(XlsxSerials.Parse(text, date1904), date1904);
}
