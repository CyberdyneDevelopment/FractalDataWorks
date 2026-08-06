using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.UI.Components.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Generic server-searchable picker for existing named configuration entities
/// (connections, datasets, calculations, pipelines, etc.).
/// </summary>
/// <typeparam name="TItem">
/// The item type rendered in the picker. The component is agnostic to the concrete type —
/// all display and key extraction is done through the caller-supplied selectors.
/// </typeparam>
/// <remarks>
/// <para>
/// Unlike <see cref="ObjectPicker{TItem}"/>, which loads a fixed item list once on first render,
/// <see cref="EntityPicker{TItem}"/> pages through an async, paged, server-side search delegate.
/// Each keystroke schedules a fresh search and cancels the in-flight one, keeping round-trips minimal.
/// </para>
/// <para>
/// The result list is rendered inside a <c>&lt;Virtualize&gt;</c> block (via
/// <see cref="Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize{TItem}"/>) so the browser
/// virtualises the scroll window even when hundreds of items match.
/// </para>
/// <para>
/// Selection is tracked by the string key produced by <see cref="KeySelector"/>
/// (or <see cref="LabelSelector"/> when not supplied). The bound <see cref="Value"/> and
/// <see cref="ValueChanged"/> follow the same two-way binding contract as
/// <see cref="OptionPicker{TTypeOption}"/> and <see cref="ObjectPicker{TItem}"/>.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor UI event handlers run on the sync context")]
public partial class EntityPicker<TItem> : IDisposable
{
    // ── Parameters: selection ──────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the key of the currently selected item (single-select).
    /// The key is whatever <see cref="KeySelector"/> returns, or the label when
    /// <see cref="KeySelector"/> is not provided.
    /// </summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the callback raised when the selected item changes.
    /// The argument is the new item key, or <c>null</c> when cleared.
    /// </summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the callback raised with the full resolved item when the selection changes.
    /// Convenient when the parent needs the item object, not just its key.
    /// </summary>
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

    // ── Parameters: source ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the async, paged, searchable source delegate.
    /// Called each time the search text changes, on initial focus (empty search), and by
    /// the Virtualize items provider for successive pages.
    /// </summary>
    /// <remarks>
    /// Signature: <c>Task&lt;IReadOnlyList&lt;TItem&gt;&gt;(string searchTerm, int skip, int take, CancellationToken)</c>.
    /// The delegate must return up to <c>take</c> items starting at offset <c>skip</c>
    /// for the given <c>searchTerm</c>. An empty search term should return the first page of
    /// all items (most-recently-used or alphabetical order is recommended). The component does NOT impose
    /// a specific sort order; that is the delegate's responsibility.
    /// </remarks>
    [Parameter, EditorRequired]
    public Func<string, int, int, CancellationToken, Task<IReadOnlyList<TItem>>> SearchSource { get; set; } = default!;

    /// <summary>
    /// Gets or sets the page size used when calling <see cref="SearchSource"/>.
    /// Defaults to 50.
    /// </summary>
    [Parameter] public int PageSize { get; set; } = 50;

    // ── Parameters: projection ─────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the function producing the primary display label for an item. Required.
    /// </summary>
    [Parameter, EditorRequired] public Func<TItem, string> LabelSelector { get; set; } = default!;

    /// <summary>
    /// Gets or sets an optional second-line display function (sublabel — e.g. type name, description).
    /// When not provided, no sublabel is rendered.
    /// </summary>
    [Parameter] public Func<TItem, string>? SubLabelSelector { get; set; }

    /// <summary>
    /// Gets or sets the function producing the stable string key for an item.
    /// Defaults to <see cref="LabelSelector"/> when not supplied (label doubles as key).
    /// </summary>
    [Parameter] public Func<TItem, string>? KeySelector { get; set; }

    // ── Parameters: presentation ───────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the label text shown above the search input. No label element renders when empty.
    /// </summary>
    [Parameter] public string? LabelText { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text for the search input.
    /// Defaults to "Search..." when not provided.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets whether a selection is required (controls placeholder tone and validation styling).
    /// </summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>
    /// Gets or sets whether the picker is disabled.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    // ── Parameters: create-new hook ───────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the optional "create new" callback. When a delegate is provided, a
    /// "+ Create new" button appears at the bottom of the results panel. The button does NOT
    /// pass a name — it simply opens the creation workflow and the parent decides what to
    /// pre-populate from the current search text.
    /// </summary>
    [Parameter] public EventCallback OnCreateNew { get; set; }

    // ── Parameters: observability ─────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the optional logger. Falls back to <see cref="NullLogger.Instance"/> when not supplied.
    /// </summary>
    // Why: NullLogger fallback is the only acceptable ?? fallback per FDW conventions.
    [Parameter] public ILogger? Logger { get; set; }

    // ── State ──────────────────────────────────────────────────────────────────────

    private IReadOnlyList<TItem> _items = [];
    private bool _isLoading;
    private bool _isPanelOpen;
    private string? _errorMessage;
    // Why: _inputText mirrors the search field value and is distinct from Value (the key) so
    // the user can type freely without clearing Value prematurely. Value is only updated on
    // an explicit item selection.
    private string _inputText = string.Empty;
    // Why: a per-search CancellationTokenSource lets each keystroke cancel the previous in-flight
    // query without cancelling the component-level CancellationToken.
    private CancellationTokenSource _searchCts = new();

    private ILogger ResolvedLogger => Logger ?? NullLogger.Instance;

    private string KeyOf(TItem item) => (KeySelector ?? LabelSelector)(item);

    // ── Lifecycle ──────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Why: when the parent resets Value to null/empty (e.g. on form reset), mirror that
        // into the input text so the field visually clears too.
        if (string.IsNullOrEmpty(Value) && !string.IsNullOrEmpty(_inputText) && _isPanelOpen)
            return; // Don't clear while the user is actively typing

        if (string.IsNullOrEmpty(Value))
            _inputText = string.Empty;
    }

    // ── Input handlers ─────────────────────────────────────────────────────────────

    private async Task HandleInput(ChangeEventArgs e)
    {
        _inputText = e.Value?.ToString() ?? string.Empty;

        // Why: if the user types after having a selection, clear Value so the parent knows
        // the old selection is no longer current.
        if (!string.IsNullOrEmpty(Value))
        {
            Value = null;
            await ValueChanged.InvokeAsync(null);
            await SelectedItemChanged.InvokeAsync(default);
        }

        _isPanelOpen = true;
        await RunSearch(_inputText);
    }

    private async Task HandleFocus()
    {
        _isPanelOpen = true;
        // Why: show the initial (empty-search) result set when the user first focuses the
        // field so they can browse without having to type anything.
        if (_items.Count == 0)
            await RunSearch(_inputText);
    }

    private async Task HandleBlur()
    {
        // Why: 200ms delay before closing lets the @onclick on result buttons fire first.
        // Without this, the panel closes before the click is processed and the item is
        // never selected. Awaiting Task.Delay resumes on the Blazor dispatcher, so
        // StateHasChanged can be called directly afterwards (no InvokeAsync needed).
        await Task.Delay(200).ConfigureAwait(true);
        _isPanelOpen = false;
        StateHasChanged();
    }

    // ── Search ─────────────────────────────────────────────────────────────────────

    private async Task RunSearch(string term)
    {
        // Why: cancel the previous in-flight search before starting a new one so we never
        // apply a stale result over a fresher one.
        await _searchCts.CancelAsync();
        _searchCts.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        if (SearchSource is null)
        {
            _errorMessage = EntityPickerLog.SearchSourceMissing(ResolvedLogger, typeof(TItem).Name).Message;
            StateHasChanged();
            return;
        }

        _isLoading = true;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            var results = await SearchSource(term, 0, PageSize, token);
            if (token.IsCancellationRequested)
                return;

            _items = results;
        }
        catch (OperationCanceledException ex)
        {
            // Why: cancellation on search change is expected; ex is observed to satisfy FDW022.
            _ = ex;
            return;
        }
        catch (Exception ex)
        {
            _errorMessage = EntityPickerLog.LoadFailed(ResolvedLogger, ex, typeof(TItem).Name, term).Message;
            _items = [];
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // ── Virtualize items provider ──────────────────────────────────────────────────

    // Why: Virtualize's ItemsProvider is called for each visible window as the user scrolls.
    // We delegate directly to SearchSource with the same search term but an incremented skip.
    private async ValueTask<ItemsProviderResult<TItem>> ProvideItems(ItemsProviderRequest request)
    {
        if (SearchSource is null)
            return new ItemsProviderResult<TItem>([], 0);

        // Why: use a CancellationToken from the request so Blazor can cancel the provider
        // call when the component is unmounted or the window changes.
        try
        {
            var results = await SearchSource(_inputText, request.StartIndex, request.Count, request.CancellationToken);
            // Why: if we get a full page back the total may be larger; expand _totalCount
            // conservatively so Virtualize renders a scrollbar. The next page-fetch will
            // correct it.
            var total = results.Count < request.Count
                ? request.StartIndex + results.Count
                : request.StartIndex + results.Count + request.Count;

            return new ItemsProviderResult<TItem>(results, total);
        }
        catch (OperationCanceledException ex)
        {
            // Why: cancellation is expected when the scroll window changes / component unmounts; observe ex.
            _ = ex;
            return new ItemsProviderResult<TItem>([], 0);
        }
        catch (Exception ex)
        {
            _errorMessage = EntityPickerLog.LoadFailed(ResolvedLogger, ex, typeof(TItem).Name, _inputText).Message;
            StateHasChanged();
            return new ItemsProviderResult<TItem>([], 0);
        }
    }

    // ── Selection ─────────────────────────────────────────────────────────────────

    private async Task SelectItem(TItem item)
    {
        Value = KeyOf(item);
        _inputText = LabelSelector(item);
        _isPanelOpen = false;

        await ValueChanged.InvokeAsync(Value);
        await SelectedItemChanged.InvokeAsync(item);
    }

    // ── Create-new ────────────────────────────────────────────────────────────────

    private Task HandleCreateNew()
    {
        _isPanelOpen = false;
        return OnCreateNew.InvokeAsync();
    }

    /// <summary>Disposes the per-search cancellation token source.</summary>
    public void Dispose()
    {
        _searchCts.Dispose();
        GC.SuppressFinalize(this);
    }
}
