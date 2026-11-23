using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>A boolean: 1 or 0.</summary>
[TypeOption(typeof(XlsxCellTypes), "Boolean")]
[ExcludeFromCodeCoverage]
public sealed class BooleanXlsxCellType : XlsxCellTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="BooleanXlsxCellType"/> class.</summary>
    public BooleanXlsxCellType()
        : base("Boolean", "b")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text, IXlsxValueKind kind, bool date1904, IReadOnlyList<string> shared)
    => string.Equals(text, "1", StringComparison.Ordinal)
        ? true
        : string.Equals(text, "0", StringComparison.Ordinal)
            ? false
            : throw new InvalidDataException($"'{text}' is not a boolean cell value (0 or 1).");
}
