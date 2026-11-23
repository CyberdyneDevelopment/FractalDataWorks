using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>An error value, kept as its text.</summary>
[TypeOption(typeof(XlsxCellTypes), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorXlsxCellType : XlsxCellTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ErrorXlsxCellType"/> class.</summary>
    public ErrorXlsxCellType()
        : base("Error", "e")
    {
    }

    /// <inheritdoc />
    public override object? Read(string text, IXlsxValueKind kind, bool date1904, IReadOnlyList<string> shared)
    => text;
}
