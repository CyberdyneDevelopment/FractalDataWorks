using System.Collections.Generic;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Components.Models;

/// <summary>
/// Concrete implementation of a column model.
/// </summary>
public sealed class ColumnModel : IColumnModel
{
    private readonly List<IComponentModel> _components = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public int Width { get; set; } = 12;

    /// <inheritdoc />
    public IReadOnlyList<IComponentModel> Components => _components.AsReadOnly();

    /// <summary>
    /// Adds a component to the column.
    /// </summary>
    /// <param name="component">The component to add.</param>
    public void AddComponent(IComponentModel component)
    {
        _components.Add(component);
    }

    /// <summary>
    /// Adds multiple components to the column.
    /// </summary>
    /// <param name="components">The components to add.</param>
    public void AddComponents(IEnumerable<IComponentModel> components)
    {
        _components.AddRange(components);
    }
}