using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a list page model.
/// </summary>
public sealed class ListPageModel : IListPageModel
{
    private readonly List<ListColumnDefinition> _columns = [];
    private readonly List<ListRowModel> _rows = [];
    private readonly List<PageAction> _listActions = [];
    private readonly List<PageAction> _rowActions = [];
    private List<int> _selectedIndices = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Title { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IListColumnDefinition> Columns => _columns;

    /// <inheritdoc />
    public IReadOnlyList<IListRowModel> Rows => _rows;

    /// <inheritdoc />
    public IPaginationState Pagination { get; set; } = new PaginationState();

    /// <inheritdoc />
    public string? SearchText { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IPageAction> ListActions => _listActions;

    /// <inheritdoc />
    public IReadOnlyList<IPageAction> RowActions => _rowActions;

    /// <inheritdoc />
    public IReadOnlyList<int> SelectedIndices
    {
        get => _selectedIndices;
        set => _selectedIndices = value.ToList();
    }

    /// <inheritdoc />
    public bool AllowMultiSelect { get; set; }

    /// <inheritdoc />
    public string EntityTypeName { get; set; } = "";

    /// <summary>
    /// Adds a column definition.
    /// </summary>
    public void AddColumn(ListColumnDefinition column) => _columns.Add(column);

    /// <summary>
    /// Adds a row to the list.
    /// </summary>
    public void AddRow(ListRowModel row) => _rows.Add(row);

    /// <summary>
    /// Clears all rows.
    /// </summary>
    public void ClearRows() => _rows.Clear();

    /// <summary>
    /// Adds a list-level action.
    /// </summary>
    public void AddListAction(PageAction action) => _listActions.Add(action);

    /// <summary>
    /// Adds a row-level action.
    /// </summary>
    public void AddRowAction(PageAction action) => _rowActions.Add(action);

    /// <summary>
    /// Gets the selected rows.
    /// </summary>
    public IEnumerable<ListRowModel> GetSelectedRows() =>
        _selectedIndices.Where(i => i >= 0 && i < _rows.Count).Select(i => _rows[i]);
}