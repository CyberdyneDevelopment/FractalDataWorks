using System;
using System.Collections.Generic;
using System.Globalization;
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

        return type switch
        {
            "s" => Shared(text, shared),
            "inlineStr" or "str" or "e" => text,
            "b" => Flag(text),
            "d" => Iso(text),
            null or "n" => Numeric(text, kind, date1904),
            _ => throw new InvalidDataException($"A cell has the type '{type}', which is not a worksheet cell type."),
        };
    }

    private static string Shared(string text, IReadOnlyList<string> shared)
    {
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= shared.Count)
        {
            throw new InvalidDataException($"A cell points at shared string '{text}', which the workbook does not have.");
        }

        return shared[index];
    }

    private static bool Flag(string text)
        => string.Equals(text, "1", StringComparison.Ordinal)
            ? true
            : string.Equals(text, "0", StringComparison.Ordinal)
                ? false
                : throw new InvalidDataException($"'{text}' is not a boolean cell value (0 or 1).");

    private static DateTime Iso(string text)
        => DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var moment)
            ? moment
            : throw new InvalidDataException($"'{text}' is not an ISO 8601 date.");

    private static object? Numeric(string text, IXlsxValueKind kind, bool date1904)
        => text.Length == 0 ? null : kind.Read(text, date1904);
}
