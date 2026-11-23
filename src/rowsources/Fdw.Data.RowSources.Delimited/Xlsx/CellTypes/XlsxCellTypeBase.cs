using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>The base of every worksheet cell type.</summary>
[ExcludeFromCodeCoverage]
public abstract class XlsxCellTypeBase : TypeOptionBase<XlsxCellTypeBase>, IXlsxCellType
{
    /// <summary>Initializes a new instance of the <see cref="XlsxCellTypeBase"/> class.</summary>
    /// <param name="name">The type's name.</param>
    /// <param name="code">The code a cell's t attribute carries for this type.</param>
    protected XlsxCellTypeBase(string name, string code)
        : base(name)
    {
        Code = code;
    }

    /// <inheritdoc />
    [TypeLookup("ByCode")]
    public string Code { get; }

    /// <inheritdoc />
    public abstract object? Read(string text, IXlsxValueKind kind, bool date1904, IReadOnlyList<string> shared);
}
