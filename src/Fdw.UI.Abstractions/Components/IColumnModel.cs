using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Components;

/// <summary>
/// Represents a column within a section.
/// </summary>
/// <remarks>
/// Columns use a 12-column grid system for responsive layouts.
/// </remarks>
public interface IColumnModel
{
    /// <summary>
    /// Gets the unique identifier for this column.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the column width (1-12 grid system).
    /// </summary>
    /// <remarks>
    /// 12 = full width, 6 = half width, 4 = third width, etc.
    /// </remarks>
    int Width { get; }

    /// <summary>
    /// Gets the components in this column.
    /// </summary>
    IReadOnlyList<IComponentModel> Components { get; }
}