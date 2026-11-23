using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A time of day or an elapsed time.</summary>
[TypeOption(typeof(XlsxValueKinds), "Time")]
[ExcludeFromCodeCoverage]
public sealed class TimeXlsxValueKind : XlsxValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeXlsxValueKind"/> class.</summary>
    public TimeXlsxValueKind() : base("Time")
    {
    }

    /// <inheritdoc />
    public override bool CoversBuiltInFormat(int formatId) => formatId is 18 or 19 or 20 or 21 or 45 or 46 or 47;

    /// <inheritdoc />
    public override object Read(string text, bool date1904)
        => XlsxSerials.Elapsed(XlsxSerials.Parse(text, date1904));
}
