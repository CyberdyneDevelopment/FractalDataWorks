using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>The string a formula yielded.</summary>
[TypeOption(typeof(XlsxCellTypes), "FormulaString")]
[ExcludeFromCodeCoverage]
public sealed class FormulaStringXlsxCellType : XlsxCellTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="FormulaStringXlsxCellType"/> class.</summary>
    public FormulaStringXlsxCellType()
        : base("FormulaString", "str")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text, IXlsxValueKind kind, bool date1904, IReadOnlyList<string> shared)
    => text;
}
