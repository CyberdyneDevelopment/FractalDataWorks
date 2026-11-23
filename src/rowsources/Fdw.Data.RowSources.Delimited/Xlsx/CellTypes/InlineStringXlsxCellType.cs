using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A string stored in the cell itself.</summary>
[TypeOption(typeof(XlsxCellTypes), "InlineString")]
[ExcludeFromCodeCoverage]
public sealed class InlineStringXlsxCellType : XlsxCellTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="InlineStringXlsxCellType"/> class.</summary>
    public InlineStringXlsxCellType()
        : base("InlineString", "inlineStr")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text, IXlsxValueKind kind, bool date1904, IReadOnlyList<string> shared)
    => text;
}
