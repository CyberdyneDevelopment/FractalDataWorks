using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// A single column in an <see cref="IBrowsePageModel"/>. Carries its own items
/// and selection state so each column can load independently (lazy drill-down).
/// </summary>
public interface IBrowseColumnModel
{
    /// <summary>
    /// Gets the column title — displayed as a header above the items list.
    /// E.g., "DataStores", "Paths in ConfigurationDb", "Containers in conn".
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the items in this column. Populated lazily by the consumer when
    /// the previous column's selection changes.
    /// </summary>
    IReadOnlyList<IBrowseItem> Items { get; }

    /// <summary>
    /// Gets or sets the currently selected item index, or -1 for none.
    /// Setting triggers the consumer's "drill to next column" logic.
    /// </summary>
    int SelectedIndex { get; set; }

    /// <summary>
    /// Gets a value indicating whether this column is loading more items.
    /// The renderer shows a spinner in the column header while true.
    /// </summary>
    bool IsLoading { get; }
}
