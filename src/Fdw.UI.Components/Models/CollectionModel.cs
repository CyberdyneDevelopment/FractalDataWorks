using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Conventions;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Components.Models;

/// <summary>
/// Data model for collection/list components that manage child items.
/// </summary>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <remarks>
/// Used for configuration properties that are lists (e.g., ConnectionOptions, PipelineSteps).
/// Supports add, remove, and reorder operations.
/// </remarks>
public sealed class CollectionModel<TItem> : IComponentModel
    where TItem : class
{
    private readonly List<TItem> _items = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string? Label { get; set; }

    /// <inheritdoc />
    public string? HelpText { get; set; }

    /// <inheritdoc />
    public bool IsRequired { get; set; }

    /// <inheritdoc />
    public bool IsReadOnly { get; set; }

    /// <inheritdoc />
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc />
    public int Order { get; set; }

    /// <summary>
    /// Gets the items in the collection.
    /// </summary>
    public IReadOnlyList<TItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Gets or sets the minimum number of items required.
    /// </summary>
    public int? MinItems { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items allowed.
    /// </summary>
    public int? MaxItems { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether items can be reordered.
    /// </summary>
    public bool AllowReorder { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether items can be added.
    /// </summary>
    public bool AllowAdd { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether items can be removed.
    /// </summary>
    public bool AllowRemove { get; set; } = true;

    /// <summary>
    /// Gets or sets the label for the "Add" button.
    /// </summary>
    public string AddButtonLabel { get; set; } = "Add";

    /// <summary>
    /// Gets or sets a factory function to create new items.
    /// </summary>
    public Func<TItem>? ItemFactory { get; set; }

    /// <summary>
    /// Gets or sets a function to validate individual items.
    /// </summary>
    public Func<TItem, int, ValidationResult>? ItemValidator { get; set; }

    /// <summary>
    /// Adds an item to the collection.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>True if added successfully, false if at max capacity.</returns>
    public bool Add(TItem item)
    {
        if (MaxItems.HasValue && _items.Count >= MaxItems.Value)
        {
            return false;
        }
        _items.Add(item);
        return true;
    }

    /// <summary>
    /// Adds a new item using the factory.
    /// </summary>
    /// <returns>The new item, or null if factory not set or at max capacity.</returns>
    public TItem? AddNew()
    {
        if (ItemFactory == null || (MaxItems.HasValue && _items.Count >= MaxItems.Value))
        {
            return null;
        }
        var item = ItemFactory();
        _items.Add(item);
        return item;
    }

    /// <summary>
    /// Removes an item from the collection.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if removed successfully.</returns>
    public bool Remove(TItem item)
    {
        return _items.Remove(item);
    }

    /// <summary>
    /// Removes an item at the specified index.
    /// </summary>
    /// <param name="index">The index of the item to remove.</param>
    /// <returns>True if removed successfully.</returns>
    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return false;
        }
        _items.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Moves an item from one index to another.
    /// </summary>
    /// <param name="fromIndex">The source index.</param>
    /// <param name="toIndex">The destination index.</param>
    /// <returns>True if moved successfully.</returns>
    public bool Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _items.Count ||
            toIndex < 0 || toIndex >= _items.Count)
        {
            return false;
        }

        var item = _items[fromIndex];
        _items.RemoveAt(fromIndex);
        _items.Insert(toIndex, item);
        return true;
    }

    /// <summary>
    /// Clears all items from the collection.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
    }

    /// <summary>
    /// Sets the items in the collection, replacing existing items.
    /// </summary>
    /// <param name="items">The items to set.</param>
    public void SetItems(IEnumerable<TItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    /// <inheritdoc />
    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // Validation logic — independent checks for required, min/max items, item validation
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (IsRequired && _items.Count == 0)
        {
            errors.Add($"{Label ?? Id} requires at least one item.");
        }

        if (MinItems.HasValue && _items.Count < MinItems.Value)
        {
            errors.Add($"{Label ?? Id} requires at least {MinItems.Value} items.");
        }

        if (MaxItems.HasValue && _items.Count > MaxItems.Value)
        {
            errors.Add($"{Label ?? Id} allows at most {MaxItems.Value} items.");
        }

        // Validate individual items
        if (ItemValidator != null)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var itemResult = ItemValidator(_items[i], i);
                if (!itemResult.IsValid)
                {
                    errors.AddRange(itemResult.Messages
                        .Where(m => string.Equals(m.Severity.Name, "Error", StringComparison.Ordinal))
                        .Select(m => $"Item {i + 1}: {m.Message}"));
                }
            }
        }

        return errors.Count > 0
            ? ValidationResult.Errors(errors)
            : ValidationResult.Success();
    }
}
