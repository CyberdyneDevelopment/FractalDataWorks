using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a row in a list page.
/// </summary>
public interface IListRowModel
{
    /// <summary>
    /// Gets the unique identifier for this row (usually the entity's ID).
    /// </summary>
    object Id { get; }

    /// <summary>
    /// Gets the cell values indexed by column ID.
    /// </summary>
    IReadOnlyDictionary<string, object?> Values { get; }

    /// <summary>
    /// Gets a value indicating whether this row is selectable.
    /// </summary>
    bool IsSelectable { get; }

    /// <summary>
    /// Gets the row status indicator (for visual styling).
    /// </summary>
    IRowStatus Status { get; }
}