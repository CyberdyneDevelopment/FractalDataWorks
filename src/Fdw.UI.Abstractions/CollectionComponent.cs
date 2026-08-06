using System;
using System.Collections.Generic;
using Fdw.UI.Abstractions.CollectionDisplayModeOptions;

namespace Fdw.UI.Abstractions;

/// <summary>
/// CRTP base for collection components.
/// Renders a list of nested components with add/remove/reorder capabilities.
/// </summary>
/// <typeparam name="TSelf">The derived collection component type (CRTP)</typeparam>
/// <typeparam name="TModel">The item model type</typeparam>
/// <typeparam name="TComponent">The component type for each item</typeparam>
public abstract class CollectionComponent<TSelf, TModel, TComponent>
    where TSelf : CollectionComponent<TSelf, TModel, TComponent>
    where TComponent : ComponentBase<TComponent, TModel>
{
    private List<TModel>? _items;

    /// <summary>
    /// The list of items in this collection.
    /// </summary>
    public IReadOnlyCollection<TModel>? Items
    {
        get => _items;
        set => _items = value != null ? new List<TModel>(value) : null;
    }

    /// <summary>
    /// Callback invoked when the items change.
    /// </summary>
    public Action<IReadOnlyCollection<TModel>>? ItemsChanged { get; set; }

    /// <summary>
    /// How to display the collection (accordion, tabs, list, grid, tree).
    /// </summary>
    public ICollectionDisplayMode? DisplayMode { get; set; }

    /// <summary>
    /// Whether users can add new items.
    /// </summary>
    public bool AllowAdd { get; set; } = true;

    /// <summary>
    /// Whether users can remove items.
    /// </summary>
    public bool AllowRemove { get; set; } = true;

    /// <summary>
    /// Whether users can reorder items.
    /// </summary>
    public bool AllowReorder { get; set; } = true;

    /// <summary>
    /// Text for the "Add" button.
    /// </summary>
    public string? AddButtonText { get; set; }

    /// <summary>
    /// Self-reference for CRTP pattern.
    /// </summary>
    protected TSelf This => (TSelf)this;

    /// <summary>
    /// Adds a new item to the collection.
    /// </summary>
    protected virtual void AddItem(TModel item)
    {
        _items ??= new List<TModel>();
        _items.Add(item);
        ItemsChanged?.Invoke(_items);
    }

    /// <summary>
    /// Removes an item at the specified index.
    /// </summary>
    protected virtual void RemoveItem(int index)
    {
        if (_items != null && index >= 0 && index < _items.Count)
        {
            _items.RemoveAt(index);
            ItemsChanged?.Invoke(_items);
        }
    }

    /// <summary>
    /// Moves an item from one index to another.
    /// </summary>
    protected virtual void MoveItem(int fromIndex, int toIndex)
    {
        if (_items != null && fromIndex >= 0 && fromIndex < _items.Count &&
            toIndex >= 0 && toIndex < _items.Count)
        {
            var item = _items[fromIndex];
            _items.RemoveAt(fromIndex);
            _items.Insert(toIndex, item);
            ItemsChanged?.Invoke(_items);
        }
    }

    /// <summary>
    /// Updates an item at the specified index.
    /// </summary>
    protected virtual void UpdateItem(int index, TModel updatedItem)
    {
        if (_items != null && index >= 0 && index < _items.Count)
        {
            _items[index] = updatedItem;
            ItemsChanged?.Invoke(_items);
        }
    }
}
