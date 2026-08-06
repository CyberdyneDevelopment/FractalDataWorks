using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Fdw.UI.Abstractions;
using Fdw.UI.Abstractions.CollectionDisplayModeOptions;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Collection component for displaying and managing collections of nested configuration objects.
/// </summary>
/// <typeparam name="TModel">The model type for collection items.</typeparam>
/// <typeparam name="TComponent">The component type used to render each item.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004", Justification = "Blazor UI components require synchronization context")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.VisualStudio.Threading.Analyzers", "VSTHRD100", Justification = "async void is correct for Blazor event handlers")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsageAnalyzers", "AsyncFixer03", Justification = "async void is correct for Blazor event handlers")]
public partial class ConfigurationCollection<TModel, TComponent>
    where TComponent : ComponentBase<TComponent, TModel>
{
    /// <summary>
    /// Gets or sets the collection of items.
    /// </summary>
    [Parameter]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0016", Justification = "Blazor two-way binding requires concrete List<T> type")]
    public List<TModel>? Items { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the collection changes.
    /// </summary>
    [Parameter] public EventCallback<List<TModel>> ItemsChanged { get; set; }

    /// <summary>
    /// Gets or sets the display mode for the collection.
    /// </summary>
    [Parameter] public ICollectionDisplayMode? DisplayMode { get; set; }

    /// <summary>
    /// Gets or sets whether users can add new items.
    /// </summary>
    [Parameter] public bool AllowAdd { get; set; } = true;

    /// <summary>
    /// Gets or sets whether users can remove items.
    /// </summary>
    [Parameter] public bool AllowRemove { get; set; } = true;

    /// <summary>
    /// Gets or sets whether users can reorder items.
    /// </summary>
    [Parameter] public bool AllowReorder { get; set; } = false;

    /// <summary>
    /// Gets or sets the text for the add button.
    /// </summary>
    [Parameter] public string? AddButtonText { get; set; }

    /// <summary>
    /// Gets or sets the factory function for creating new items.
    /// </summary>
    [Parameter] public Func<TModel>? ItemFactory { get; set; }

    private HashSet<int> _expandedItems = new();

    private void ToggleItem(int index)
    {
        // CA1868: HashSet.Remove returns bool indicating if item was present
        if (!_expandedItems.Remove(index))
        {
            _expandedItems.Add(index);
        }
    }

    private bool IsItemExpanded(int index)
    {
        return _expandedItems.Contains(index);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0002", Justification = "Ordinal comparison not needed for component parameter dictionary keys")]
    private Dictionary<string, object> GetComponentParameters(TModel item, int index)
    {
        return new Dictionary<string, object>
        {
            [nameof(ComponentBase<TComponent, TModel>.Value)] = item!,
            [nameof(ComponentBase<TComponent, TModel>.ValueChanged)] = EventCallback.Factory.Create<TModel>(this, async updatedItem =>
            {
                if (Items != null && index < Items.Count)
                {
                    Items[index] = updatedItem;
                    await ItemsChanged.InvokeAsync(Items);
                }
            })
        };
    }

    private async void AddNewItem()
    {
        Items ??= new List<TModel>();

        var newItem = ItemFactory != null
            ? ItemFactory()
            : Activator.CreateInstance<TModel>();

        Items.Add(newItem);
        _expandedItems.Add(Items.Count - 1); // Expand newly added item

        await ItemsChanged.InvokeAsync(Items);
        StateHasChanged();
    }

    private async void RemoveItem(int index)
    {
        if (Items != null && index >= 0 && index < Items.Count)
        {
            Items.RemoveAt(index);
            _expandedItems.Remove(index);

            // Adjust expanded indices
            var adjustedExpanded = _expandedItems
                .Where(i => i > index)
                .Select(i => i - 1)
                .ToHashSet();
            _expandedItems = adjustedExpanded;

            await ItemsChanged.InvokeAsync(Items);
            StateHasChanged();
        }
    }
}
