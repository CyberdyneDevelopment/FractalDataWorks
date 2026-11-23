using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>One worksheet cell type, as a cell's t attribute names it, and how a cell of that type is read.</summary>
public interface IXlsxCellType : ITypeOption<int, XlsxCellTypeBase>
{
    /// <summary>Gets the code a cell's t attribute carries for this type.</summary>
    string Code { get; }

    /// <summary>Reads the stored text of a cell of this type.</summary>
    /// <param name="text">The stored text: a v element's or an inline string's.</param>
    /// <param name="kind">What the cell's number format makes a number.</param>
    /// <param name="date1904">Whether the workbook counts dates from 1904.</param>
    /// <param name="shared">The shared string table.</param>
    /// <returns>The value; null for an empty number cell.</returns>
    /// <exception cref="System.IO.InvalidDataException">The text is not what the cell's type says.</exception>
    object? Read(string text, IXlsxValueKind kind, bool date1904, IReadOnlyList<string> shared);
}
