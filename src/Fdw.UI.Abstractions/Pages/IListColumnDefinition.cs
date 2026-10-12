namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Defines a column in a list page.
/// </summary>
public interface IListColumnDefinition
{
    /// <summary>
    /// Gets the column identifier (property name).
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the column display header.
    /// </summary>
    string Header { get; }

    /// <summary>
    /// Gets the column width (percentage or fixed).
    /// </summary>
    int? Width { get; }

    /// <summary>
    /// Gets a value indicating whether this column is sortable.
    /// </summary>
    bool IsSortable { get; }

    /// <summary>
    /// Gets a value indicating whether this column is visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets the text alignment for this column.
    /// </summary>
    IColumnAlignment Alignment { get; }

    /// <summary>
    /// Gets the format string for displaying values (e.g., "N2" for numbers).
    /// </summary>
    string? FormatString { get; }
}