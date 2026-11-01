using Fdw.Collections;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>What a numeric Excel cell holds, as its number format says, and how its stored text is read.</summary>
public interface IXlsxValueKind : ITypeOption<int, XlsxValueKindBase>
{
    /// <summary>Reads a numeric cell's stored text as the value this kind holds.</summary>
    /// <param name="text">The stored text of a v element; never empty.</param>
    /// <param name="date1904">Whether the workbook counts dates from 1904.</param>
    /// <returns>The value: a whole number, a decimal or double, a date and time, a time of day or an elapsed span.</returns>
    /// <exception cref="System.IO.InvalidDataException">The text is not what the kind says.</exception>
    object Read(string text, bool date1904);
}
