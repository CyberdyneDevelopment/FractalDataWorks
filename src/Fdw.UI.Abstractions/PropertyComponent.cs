using System;

namespace Fdw.UI.Abstractions;

/// <summary>
/// CRTP base for property-level components.
/// Renders a single property of a model.
/// </summary>
/// <typeparam name="TSelf">The derived property component type (CRTP)</typeparam>
/// <typeparam name="TProperty">The property value type</typeparam>
public abstract class PropertyComponent<TSelf, TProperty> : IPropertyComponent
    where TSelf : PropertyComponent<TSelf, TProperty>
{
    /// <summary>
    /// The current value of the property.
    /// </summary>
    public TProperty? Value { get; set; }

    /// <summary>
    /// Callback invoked when the property value changes.
    /// </summary>
    public Action<TProperty>? ValueChanged { get; set; }

    /// <summary>
    /// Metadata about the property (label, help text, validation, etc.)
    /// </summary>
    public PropertyMetadata? Metadata { get; set; }

    /// <summary>
    /// Whether this property is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Self-reference for CRTP pattern.
    /// </summary>
    protected TSelf This => (TSelf)this;

    /// <summary>
    /// Invokes the value changed callback.
    /// </summary>
    protected virtual void OnValueChanged(TProperty? newValue)
    {
        Value = newValue;
        ValueChanged?.Invoke(newValue!);
    }
}
