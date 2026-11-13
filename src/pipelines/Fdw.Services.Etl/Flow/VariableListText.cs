using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// How a list held in a variable is written where a variable becomes text (FDW-5347): as <c>"A","B","C"</c>, each item in double quotes
/// and the items separated by commas.
/// </summary>
/// <remarks>
/// <para>
/// Why the value decides and no setting: a list is a list wherever a variable names it, in a source's path, in a parameter's text or in a script's
/// <c>$(Name)</c> token, so every place that renders a variable calls this and a new place gets the same answer. A value that is text or a
/// single value is not a list and renders as it always did; what counts as a list is what a For each reads as one.
/// </para>
/// <para>
/// Why an item with a double quote is refused: there is no escaping and no fallback. A script's token is put into SQL text, and an item
/// that could close its own quotes would let a value write SQL; the same rule everywhere keeps one answer. A null item is refused too, so
/// nothing is written for a value that is not there, and so is an empty list.
/// </para>
/// </remarks>
public static class VariableListText
{
    private const char Quote = '"';

    private const char Separator = ',';

    /// <summary>Says whether a value is a list: something that can be walked, other than text.</summary>
    /// <param name="value">The value of a variable.</param>
    /// <returns><see langword="true"/> for a list, <see langword="false"/> for text, a single value or nothing.</returns>
    public static bool IsList(object? value) => value is not string && value is IEnumerable;

    /// <summary>Writes a list as quoted, comma-separated text.</summary>
    /// <param name="list">The list; an empty list is refused, because no text stands for it (a script's <c>IN ()</c> is not SQL).</param>
    /// <param name="text">The text, <c>"A","B","C"</c>, when the list can be written; otherwise null.</param>
    /// <returns><see langword="false"/> when the list is empty or an item is null or holds a double quote; nothing is written then.</returns>
    public static bool TryRender(IEnumerable list, [NotNullWhen(true)] out string? text)
    {
        var rendered = new StringBuilder();
        foreach (var item in list)
        {
            var itemText = item is null ? null : Convert.ToString(item, CultureInfo.InvariantCulture);
            if (itemText is null || itemText.Contains(Quote, StringComparison.Ordinal))
            {
                text = null;
                return false;
            }

            if (rendered.Length > 0)
            {
                rendered.Append(Separator);
            }

            rendered.Append(Quote).Append(itemText).Append(Quote);
        }

        if (rendered.Length == 0)
        {
            text = null;
            return false;
        }

        text = rendered.ToString();
        return true;
    }
}
