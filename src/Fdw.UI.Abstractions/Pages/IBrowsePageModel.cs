using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Miller-column browse page — N linked columns where selecting an item in column
/// i loads / populates column i+1. The renderer shows a sliding window of columns
/// (default 3) centred on the active one, with a breadcrumb for deeper navigation.
/// </summary>
/// <remarks>
/// This is the page model the <c>fdw browse</c> verb drives. Consumers build a
/// single root column and populate subsequent columns as the user drills in.
/// Rendering is agnostic of what the columns contain — they can be DataStores,
/// paths, containers, or rows of actual data.
/// </remarks>
// Why: List + Detail don't express drill-down navigation. Tree is hierarchical
// but doesn't have the column pane layout. Miller-column is the right shape for
// "pick a DataStore, then a path, then a container, then preview its rows".
public interface IBrowsePageModel
{
    /// <summary>Gets the unique identifier for this page.</summary>
    string Id { get; }

    /// <summary>Gets the page title.</summary>
    string Title { get; }

    /// <summary>Gets the page description.</summary>
    string? Description { get; }

    /// <summary>
    /// Gets the breadcrumb of selected names, one per resolved column. Always in
    /// sync with the selections in <see cref="Columns"/>.
    /// </summary>
    IReadOnlyList<string> Breadcrumb { get; }

    /// <summary>
    /// Gets the columns, ordered root-first. The renderer displays a trailing
    /// window (typically 3) so very deep drills scroll left as the user descends.
    /// </summary>
    IReadOnlyList<IBrowseColumnModel> Columns { get; }

    /// <summary>
    /// Gets or sets the zero-based index of the currently focused column. Keyboard
    /// left/right navigation moves this index without touching selections.
    /// </summary>
    int ActiveColumnIndex { get; set; }

    /// <summary>
    /// Gets the optional data preview — when the leaf selection represents a
    /// table/view, the renderer shows the preview rows in a pane below the
    /// columns. Null when the leaf is not previewable.
    /// </summary>
    IListPageModel? PreviewRows { get; }

    /// <summary>
    /// Gets the available page-level actions (Refresh, Filter, Open, Back, Quit).
    /// </summary>
    IReadOnlyList<IPageAction> PageActions { get; }
}
