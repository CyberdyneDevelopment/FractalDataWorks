using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A number, a date or a time, as the cell's number format says. A cell with no t attribute is this type.</summary>
[TypeOption(typeof(XlsxCellTypes), "Number")]
[ExcludeFromCodeCoverage]
public sealed class NumberXlsxCellType : XlsxCellTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NumberXlsxCellType"/> class.</summary>
    public NumberXlsxCellType()
        : base("Number", "n")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text, IXlsxValueKind kind, bool date1904, IReadOnlyList<string> shared)
    => text.Length == 0 ? null : kind.Read(text, date1904);
}
