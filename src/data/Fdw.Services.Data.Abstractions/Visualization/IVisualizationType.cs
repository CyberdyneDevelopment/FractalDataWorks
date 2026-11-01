using System;
using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Represents a visualization type for data presentation.
/// </summary>
public interface IVisualizationType : ITypeOption<int, VisualizationTypeBase>
{
    /// <summary>
    /// Gets the display name for the visualization type.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the icon identifier for the visualization type.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Gets the data types supported by this visualization.
    /// </summary>
    IReadOnlyList<Type> SupportedDataTypes { get; }

    /// <summary>
    /// Determines whether this visualization type can visualize the given column types.
    /// </summary>
    /// <param name="columnTypes">The column data types to check.</param>
    /// <returns>True if this visualization type can handle the given column types.</returns>
    bool CanVisualize(IReadOnlyList<string> columnTypes);

    /// <summary>
    /// Gets the default configuration for this visualization type.
    /// </summary>
    /// <returns>A default <see cref="VisualizationConfig"/> for this visualization type.</returns>
    VisualizationConfig GetDefaultConfiguration();
}
