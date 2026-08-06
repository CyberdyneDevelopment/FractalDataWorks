using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Components;

/// <summary>
/// Represents a section within a page.
/// </summary>
/// <remarks>
/// Sections group related fields together and can be collapsible.
/// </remarks>
public interface ISectionModel
{
    /// <summary>
    /// Gets the unique identifier for this section.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the section title.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the section description.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets a value indicating whether this section can be collapsed.
    /// </summary>
    bool IsCollapsible { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this section is expanded.
    /// </summary>
    bool IsExpanded { get; set; }

    /// <summary>
    /// Gets a value indicating whether this section is visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets the columns in this section.
    /// </summary>
    IReadOnlyList<IColumnModel> Columns { get; }

    /// <summary>
    /// Gets all components in this section (flattened across columns).
    /// </summary>
    IEnumerable<IComponentModel> AllComponents { get; }
}