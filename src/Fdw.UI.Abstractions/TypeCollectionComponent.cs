using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Collections;

namespace Fdw.UI.Abstractions;

/// <summary>
/// CRTP base for TypeCollection selection components.
/// Provides type-safe dropdown/selection for TypeCollection options.
/// </summary>
/// <typeparam name="TSelf">The derived component type (CRTP)</typeparam>
/// <typeparam name="TCollection">The TypeCollection type</typeparam>
/// <typeparam name="TOption">The TypeOption interface</typeparam>
public abstract class TypeCollectionComponent<TSelf, TCollection, TOption>
    where TSelf : TypeCollectionComponent<TSelf, TCollection, TOption>
    where TCollection : class
    where TOption : class, ITypeOption
{
    /// <summary>
    /// The currently selected option ID.
    /// </summary>
    public int SelectedId { get; set; }

    /// <summary>
    /// Callback invoked when selection changes.
    /// </summary>
    public Action<int>? SelectedIdChanged { get; set; }

    /// <summary>
    /// Placeholder text when nothing is selected.
    /// </summary>
    public string? Placeholder { get; set; } = "-- Select --";

    /// <summary>
    /// Whether this is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Property metadata (label, help text).
    /// </summary>
    public PropertyMetadata? Metadata { get; set; }

    /// <summary>
    /// Self-reference for CRTP pattern.
    /// </summary>
    protected TSelf This => (TSelf)this;

    /// <summary>
    /// Gets the TypeCollection instance.
    /// Uses reflection to get static Instance property.
    /// Derived classes can override to provide direct access.
    /// </summary>
    protected virtual TCollection? GetCollectionInstance()
    {
        var instanceProperty = typeof(TCollection).GetProperty("Instance",
            BindingFlags.Public | BindingFlags.Static);
        return instanceProperty?.GetValue(null) as TCollection;
    }

    /// <summary>
    /// Gets all available options.
    /// Uses reflection to call the generated All() method.
    /// Derived classes should override to provide direct access.
    /// </summary>
    protected virtual IEnumerable<TOption> GetOptions()
    {
        var collection = GetCollectionInstance();
        if (collection == null)
        {
            return Enumerable.Empty<TOption>();
        }

        var allMethod = typeof(TCollection).GetMethod("All",
            BindingFlags.Public | BindingFlags.Static);

        if (allMethod != null)
        {
            var result = allMethod.Invoke(null, null);
            if (result is IEnumerable<TOption> options)
            {
                return options;
            }
        }

        return Enumerable.Empty<TOption>();
    }

    /// <summary>
    /// Gets the currently selected option.
    /// Uses reflection to call the generated Id() method.
    /// Derived classes should override to provide direct access.
    /// </summary>
    protected virtual TOption? GetSelectedOption()
    {
        var idMethod = typeof(TCollection).GetMethod("Id",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(int) },
            null);

        if (idMethod != null)
        {
            var result = idMethod.Invoke(null, new object[] { SelectedId });
            return result as TOption;
        }

        return null;
    }

    /// <summary>
    /// Handles selection change.
    /// </summary>
    protected virtual void OnSelectionChanged(int newId)
    {
        SelectedId = newId;
        SelectedIdChanged?.Invoke(newId);
    }
}
