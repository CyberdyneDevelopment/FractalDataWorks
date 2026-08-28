using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.UI.Components.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Generic, dependency-light picker over any typed set of items. Unlike
/// <see cref="OptionPicker{TTypeOption}"/> (which is constrained to <c>ITypeOption</c>), this picker
/// works over any <typeparamref name="TItem"/> by way of caller-supplied
/// <see cref="LabelSelector"/> and <see cref="KeySelector"/> functions.
/// </summary>
/// <typeparam name="TItem">The item type rendered in the picker.</typeparam>
/// <remarks>
/// Items come from either:
/// <list type="bullet">
///   <item><description><see cref="Items"/> — a pre-resolved static list (assigned synchronously).</description></item>
///   <item><description><see cref="ItemsSource"/> — an async loader invoked once on first render
///   (ignored when <see cref="Items"/> is supplied).</description></item>
/// </list>
/// Selection is tracked by the string key produced by <see cref="KeySelector"/>. The bound
/// <see cref="Value"/>/<see cref="Values"/> are also keys, so the component stays headless and
/// dependency-light — consumers control item rendering through <see cref="LabelSelector"/>.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor UI event handlers run on the sync context")]
public partial class ObjectPicker<TItem>
{
    // ── Parameters: selection ──────────────────────────────────────────────────────

    /// <summary>Gets or sets the selected item's key (single-select mode).</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Gets or sets the callback raised when the single-select value changes (the new key, or <c>null</c>).</summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Gets or sets the selected item keys (multi-select mode, when <see cref="AllowMultiple"/> is <c>true</c>).</summary>
    [Parameter] public IReadOnlyList<string> Values { get; set; } = [];

    /// <summary>Gets or sets the callback raised when the multi-select values change.</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> ValuesChanged { get; set; }

    /// <summary>Gets or sets the callback raised with the resolved item(s) when the selection changes.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> SelectedItemsChanged { get; set; }

    // ── Parameters: items source ───────────────────────────────────────────────────

    /// <summary>Gets or sets a pre-resolved static item list. When non-null, <see cref="ItemsSource"/> is ignored.</summary>
    [Parameter] public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>Gets or sets an async loader that resolves items on first render. Ignored when <see cref="Items"/> is supplied.</summary>
    [Parameter] public Func<Task<IReadOnlyList<TItem>>>? ItemsSource { get; set; }

    // ── Parameters: projection ─────────────────────────────────────────────────────

    /// <summary>Gets or sets the function producing the display label for an item. Required.</summary>
    [Parameter, EditorRequired] public Func<TItem, string> LabelSelector { get; set; } = default!;

    /// <summary>
    /// Gets or sets the function producing the stable string key for an item.
    /// Defaults to <see cref="LabelSelector"/> when not supplied (label doubles as key).
    /// </summary>
    [Parameter] public Func<TItem, string>? KeySelector { get; set; }

    // ── Parameters: presentation ───────────────────────────────────────────────────

    /// <summary>Gets or sets the label text shown above the select. No label element renders when empty.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Gets or sets the placeholder text for the blank option (single-select).</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Gets or sets whether a non-empty selection is required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Gets or sets whether the picker is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets or sets whether multiple items may be selected.</summary>
    [Parameter] public bool AllowMultiple { get; set; }

    // ── Parameters: inline create ──────────────────────────────────────────────────

    /// <summary>Gets or sets whether the inline "+ Add new" affordance is shown. Requires <see cref="OnCreate"/>.</summary>
    [Parameter] public bool AllowCreate { get; set; }

    /// <summary>Gets or sets the inline-create hook. Receives the typed name; the parent performs creation and refreshes items.</summary>
    [Parameter] public EventCallback<string> OnCreate { get; set; }

    /// <summary>Gets or sets the placeholder text for the inline-create input.</summary>
    [Parameter] public string? CreatePlaceholder { get; set; }

    /// <summary>Gets or sets the optional logger. Falls back to <see cref="NullLogger.Instance"/>.</summary>
    [Parameter] public ILogger? Logger { get; set; }

    // ── State ──────────────────────────────────────────────────────────────────────

    private IReadOnlyList<TItem> _items = [];
    private bool _isLoading;
    private string? _errorMessage;
    private bool _initialized;
    private IReadOnlyList<TItem>? _lastItems;

    private string? _selectedKey;
    private string[] _selectedKeys = [];

    private bool _isCreating;
    private string _newLabel = string.Empty;

    private ILogger ResolvedLogger => Logger ?? NullLogger.Instance;

    private string KeyOf(TItem item) => (KeySelector ?? LabelSelector)(item);

    // ── Lifecycle ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Items is not null && !ReferenceEquals(Items, _lastItems))
        {
            _lastItems = Items;
            _items = Items;
        }

        _selectedKey = Value;
        _selectedKeys = Values?.ToArray() ?? [];
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _initialized || Items is not null || ItemsSource is null)
            return;

        _initialized = true;
        await LoadFromSource();
    }

    // ── Loading ────────────────────────────────────────────────────────────────────

    private async Task LoadFromSource()
    {
        if (ItemsSource is null) return;

        _isLoading = true;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            _items = await ItemsSource();
        }
        catch (OperationCanceledException ex)
        {
            _ = ex;
            return;
        }
        catch (Exception ex)
        {
            _errorMessage = ObjectPickerLog.LoadFailed(ResolvedLogger, ex, typeof(TItem).Name).Message;
            _items = [];
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // ── Selection handlers ───────────────────────────────────────────────────────────

    private async Task HandleSingleChange(string? newKey)
    {
        _selectedKey = string.IsNullOrEmpty(newKey) ? null : newKey;
        Value = _selectedKey;
        await ValueChanged.InvokeAsync(_selectedKey);
        await RaiseSelectedItems(_selectedKey is null ? [] : new[] { _selectedKey });
    }

    private async Task HandleMultiChange(string[]? newKeys)
    {
        _selectedKeys = newKeys ?? [];
        Values = _selectedKeys;
        await ValuesChanged.InvokeAsync(_selectedKeys);
        await RaiseSelectedItems(_selectedKeys);
    }

    private Task RaiseSelectedItems(IReadOnlyList<string> keys)
    {
        if (!SelectedItemsChanged.HasDelegate)
            return Task.CompletedTask;

        var resolved = _items
            .Where(i => keys.Contains(KeyOf(i), StringComparer.Ordinal))
            .ToList();
        return SelectedItemsChanged.InvokeAsync(resolved);
    }

    // ── Inline create ────────────────────────────────────────────────────────────────

    private async Task InvokeCreate()
    {
        if (string.IsNullOrWhiteSpace(_newLabel))
            return;

        await OnCreate.InvokeAsync(_newLabel.Trim());
        _newLabel = string.Empty;
        _isCreating = false;
    }

    private void CancelCreate()
    {
        _isCreating = false;
        _newLabel = string.Empty;
    }
}
