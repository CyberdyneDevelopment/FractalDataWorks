using System.Collections.Generic;
using System.IO;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>Turns a cell's stored text into the value it holds, by the cell's type and number format.</summary>
internal static class XlsxCellValues
{
    /// <summary>Converts a cell.</summary>
    /// <param name="type">The cell's t attribute; null is a number.</param>
    /// <param name="text">The stored text: a v element's, an inline string's, or null for a cell with neither.</param>
    /// <param name="kind">What the cell's number format makes a number.</param>
    /// <param name="date1904">Whether the workbook counts dates from 1904.</param>
    /// <param name="shared">The shared string table.</param>
    /// <returns>The value; null for an empty cell.</returns>
    /// <exception cref="InvalidDataException">The text is not what the cell's type says.</exception>
    internal static object? Convert(string? type, string? text, IXlsxValueKind kind, bool date1904, IReadOnlyList<string> shared)
    {
        if (text is null)
        {
            return null;
        }

        var cellType = type is null ? XlsxCellTypes.Number : XlsxCellTypes.ByCode(type);
        return ReferenceEquals(cellType, XlsxCellTypes.NotFound)
            ? throw new InvalidDataException($"A cell has the type '{type}', which is not a worksheet cell type.")
            : cellType.Read(text, kind, date1904, shared);
    }
}
