using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A string held in the workbook's shared string table: the cell stores its position.</summary>
[TypeOption(typeof(XlsxCellTypes), "SharedString")]
[ExcludeFromCodeCoverage]
public sealed class SharedStringXlsxCellType : XlsxCellTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SharedStringXlsxCellType"/> class.</summary>
    public SharedStringXlsxCellType()
        : base("SharedString", "s")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text, IXlsxValueKind kind, bool date1904, IReadOnlyList<string> shared)
    {
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= shared.Count)
        {
            throw new InvalidDataException($"A cell points at shared string '{text}', which the workbook does not have.");
        }

        return shared[index];
    }
}
