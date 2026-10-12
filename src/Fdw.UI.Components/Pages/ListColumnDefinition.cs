using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a list column definition.
/// </summary>
public sealed class ListColumnDefinition : IListColumnDefinition
{
    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Header { get; set; } = "";

    /// <inheritdoc />
    public int? Width { get; set; }

    /// <inheritdoc />
    public bool IsSortable { get; set; } = true;

    /// <inheritdoc />
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc />
    public IColumnAlignment Alignment { get; set; } = ColumnAlignments.Left;

    /// <inheritdoc />
    public string? FormatString { get; set; }

    /// <summary>
    /// Creates a new column definition.
    /// </summary>
    public static ListColumnDefinition Create(string id, string header, int? width = null) =>
        new() { Id = id, Header = header, Width = width };
}