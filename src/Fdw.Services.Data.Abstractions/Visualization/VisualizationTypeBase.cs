using System;
using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Base class for visualization type TypeOptions using CRTP pattern.
/// </summary>
public abstract class VisualizationTypeBase : TypeOptionBase<int, VisualizationTypeBase>, IVisualizationType
{
    /// <summary>
    /// Parameterless constructor required by the TypeCollection source generator for the Empty sentinel.
    /// </summary>
    protected VisualizationTypeBase()
        : base(0, "Empty", "VisualizationTypes:Empty", "Empty", "Empty visualization type", "Visualization")
    {
        Icon = string.Empty;
        SupportedDataTypes = Array.Empty<Type>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualizationTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The visualization type name.</param>
    /// <param name="displayName">The display name for the visualization type.</param>
    /// <param name="icon">The icon identifier.</param>
    /// <param name="supportedDataTypes">The data types this visualization supports.</param>
    protected VisualizationTypeBase(
        int id,
        string name,
        string displayName,
        string icon,
        IReadOnlyList<Type> supportedDataTypes)
        : base(id, name, $"VisualizationTypes:{name}", displayName, $"{displayName} visualization type", "Visualization")
    {
        Icon = icon;
        SupportedDataTypes = supportedDataTypes;
    }

    /// <inheritdoc/>
    public string Icon { get; }

    /// <inheritdoc/>
    public IReadOnlyList<Type> SupportedDataTypes { get; }

    /// <inheritdoc/>
    public abstract bool CanVisualize(IReadOnlyList<string> columnTypes);

    /// <inheritdoc/>
    public abstract VisualizationConfig GetDefaultConfiguration();
}
