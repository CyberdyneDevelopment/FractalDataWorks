namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// A single item in an <see cref="IBrowseColumnModel"/> — the thing the user
/// can highlight and select. Items carry a name/label and an opaque payload
/// the consumer uses to decide what the next column should show.
/// </summary>
public interface IBrowseItem
{
    /// <summary>The display label shown in the column.</summary>
    string Label { get; }

    /// <summary>Optional secondary text shown beside the label (type, count, etc.).</summary>
    string? Detail { get; }

    /// <summary>
    /// True when selecting this item should trigger a drill-down (the usual case).
    /// False for leaf items that have no child column.
    /// </summary>
    bool HasChildren { get; }

    /// <summary>
    /// Arbitrary payload associated with this item (e.g., the raw DataStoreConfiguration).
    /// The renderer doesn't inspect it; consumers use it when building the next column.
    /// </summary>
    object? Payload { get; }
}
