using System.Linq;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Delimited.Abstractions;

/// <summary>Options for the Excel workbook writer.</summary>
public sealed class XlsxRowWriterOptions : RowWriterOptions
{
    /// <summary>The most rows a worksheet holds, the header row included, in a workbook Excel opens.</summary>
    public const int MaxRowsPerSheet = 1_048_576;

    /// <summary>The most columns a worksheet holds.</summary>
    public const int MaxColumns = 16_384;

    /// <summary>The most characters a worksheet name holds.</summary>
    public const int MaxSheetNameLength = 31;

    /// <summary>Gets whether Excel forbids the character in a worksheet name: <c>[ ] : * ? / \</c>.</summary>
    /// <param name="character">The character.</param>
    public static bool IsForbiddenInSheetName(char character)
        => character is '[' or ']' or ':' or '*' or '?' or '/' or '\\';

    /// <summary>Gets whether the text is a worksheet name Excel can hold: 1 to 31 characters, none forbidden, not blank.</summary>
    /// <param name="name">The name.</param>
    public static bool IsValidSheetName(string? name)
        => !string.IsNullOrWhiteSpace(name) && name!.Length <= MaxSheetNameLength && !name.Any(IsForbiddenInSheetName);

    /// <summary>Gets or sets the first worksheet's name; a further one is named after it and numbered. Excel limits a name to 31 characters and forbids <c>[ ] : * ? / \</c>; the writer replaces them.</summary>
    public string SheetName { get; set; } = "Data";

    /// <summary>Gets or sets the rows a worksheet holds, the header row included, before the rows continue on a further worksheet. At least 2 and at most <see cref="MaxRowsPerSheet"/>.</summary>
    public int RowsPerSheet { get; set; } = MaxRowsPerSheet;
}
