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
    public override object Read(string text, bool date1904)
        => XlsxSerials.Moment(XlsxSerials.Parse(text, date1904), date1904);
}
