using System;
using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a list/browse page for viewing collections of items.
/// </summary>
/// <remarks>
/// List pages display tabular data with support for:
/// - Pagination
/// - Sorting by columns
/// - Filtering/searching
/// - Row selection
/// - Actions (create, edit, delete, etc.)
/// </remarks>
public interface IListPageModel
{
    /// <summary>
    /// Gets the unique identifier for this page.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the page title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the page description.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the column definitions for the list.
    /// </summary>
    IReadOnlyList<IListColumnDefinition> Columns { get; }

    /// <summary>
    /// Gets the current items being displayed.
    /// </summary>
    IReadOnlyList<IListRowModel> Rows { get; }

    /// <summary>
    /// Gets the pagination state.
    /// </summary>
    IPaginationState Pagination { get; }

    /// <summary>
    /// Gets or sets the current search/filter text.
    /// </summary>
    string? SearchText { get; set; }

    /// <summary>
    /// Gets the available actions for the list (e.g., Create New).
    /// </summary>
    IReadOnlyList<IPageAction> ListActions { get; }

    /// <summary>
    /// Gets the available actions for each row (e.g., Edit, Delete).
    /// </summary>
    IReadOnlyList<IPageAction> RowActions { get; }

    /// <summary>
    /// Gets or sets the currently selected row indices.
    /// </summary>
    IReadOnlyList<int> SelectedIndices { get; set; }

    /// <summary>
    /// Gets a value indicating whether multiple selection is allowed.
    /// </summary>
    bool AllowMultiSelect { get; }

    /// <summary>
    /// Gets the entity type name being listed (e.g., "Connection", "Pipeline").
    /// </summary>
    string EntityTypeName { get; }
}