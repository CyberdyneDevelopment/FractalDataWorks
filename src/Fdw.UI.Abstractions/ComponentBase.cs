using System;
using System.Collections.Generic;
using Fdw.UI.Abstractions.RenderModeOptions;

namespace Fdw.UI.Abstractions;

/// <summary>
/// CRTP base for all UI components across any framework.
/// TSelf enables self-referential operations and type-safe chaining.
/// TModel is the data model being edited/displayed.
/// </summary>
/// <typeparam name="TSelf">The derived component type (CRTP pattern)</typeparam>
/// <typeparam name="TModel">The model type being rendered</typeparam>
public abstract class ComponentBase<TSelf, TModel>
    where TSelf : ComponentBase<TSelf, TModel>
{
    /// <summary>
    /// The model value being edited or displayed.
    /// </summary>
    public TModel? Value { get; set; }

    /// <summary>
    /// Callback invoked when the value changes.
    /// Framework-specific: Blazor uses EventCallback, React uses Action, etc.
    /// </summary>
    public Action<TModel>? ValueChanged { get; set; }

    /// <summary>
    /// Rendering mode for this component.
    /// </summary>
    public IRenderMode? RenderMode { get; set; }

    /// <summary>
    /// Self-reference for CRTP pattern.
    /// Enables type-safe extension methods and fluent APIs.
    /// </summary>
    protected TSelf This => (TSelf)this;

    /// <summary>
    /// Gets all property-level components for this model.
    /// Implemented by derived classes or source generators.
    /// </summary>
    protected abstract IEnumerable<IPropertyComponent> GetPropertyComponents();

    /// <summary>
    /// Validates whether this component can contain a child component of type TChild.
    /// Compile-time validated by source generators.
    /// </summary>
    /// <typeparam name="TChild">The child model type</typeparam>
    /// <returns>True if this component can contain TChild</returns>
    protected abstract bool CanContain<TChild>();

    /// <summary>
    /// Gets metadata about this component.
    /// Used by source generators and framework adapters.
    /// </summary>
    public virtual ComponentMetadata GetMetadata()
    {
        return new ComponentMetadata
        {
            ComponentType = typeof(TSelf).Name,
            ModelType = typeof(TModel).Name,
            RenderMode = RenderMode
        };
    }
}
