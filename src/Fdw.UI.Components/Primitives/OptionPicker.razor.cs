using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.UI.Components.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Generic picker component for any TypeCollection. Replaces all bespoke *Picker.razor
/// components that rendered a select over a TypeCollection.
/// </summary>
/// <typeparam name="TTypeOption">
/// The TypeOption type. Must implement <see cref="ITypeOption"/> so the picker can enumerate names.
/// </typeparam>
/// <remarks>
/// Consumers provide either:
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="StaticOptions"/> — a pre-resolved enumerable (pass <c>MyTypes.All()</c>).
///       Assigned synchronously from parameters; no async load occurs.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="Source"/> — an async factory invoked once on first render
///       (for DB-backed MutableTypeCollections). Ignored when <see cref="StaticOptions"/> is provided.
///     </description>
///   </item>
/// </list>
/// Two-way binding uses the option <c>Name</c> as the string key, consistent with
/// TypeCollection <c>ByName()</c> lookup.
///
/// Optional features (backward-compatible additions):
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="Searchable"/> + <see cref="SearchPlaceholder"/> — renders a type-ahead filter
///       input above the list; all filtering is done client-side over the already-loaded option list.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="GroupSelector"/> — a func that returns the group label for an option. When
///       supplied the picker renders a grouped, collapsible card list instead of a plain select.
///       Designed to match the TransformTypeSelector UX. The grouped path supports
///       <see cref="Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize{TItem}"/> for
///       very large per-group lists (HTML forbids Virtualize inside a <c>&lt;select&gt;</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="VirtualizeThreshold"/> — the per-group option count above which a single
///       group switches to a Virtualize block. Defaults to 50.
///     </description>
///   </item>
/// </list>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor UI event handlers run on sync context")]
public partial class OptionPicker<TTypeOption>
    where TTypeOption : class, ITypeOption
{
    // ── Parameters: selection (original API — unchanged) ──────────────────────────

    /// <summary>
    /// Gets or sets the current value, bound to the selected option's <see cref="ITypeOption.Name"/>.
    /// </summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the callback raised when the user selects a new option.
    /// The string argument is the selected option name, or <c>null</c> when cleared.
    /// </summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets a pre-resolved enumerable of options.
    /// Use this for static TypeCollections: pass <c>MyTypes.All()</c>.
    /// When non-null, <see cref="Source"/> is ignored.
    /// </summary>
    [Parameter] public IEnumerable<TTypeOption>? StaticOptions { get; set; }

    /// <summary>
    /// Gets or sets an async factory that resolves the option list at runtime.
    /// Used for DB-backed MutableTypeCollections. Ignored when <see cref="StaticOptions"/> is provided.
    /// </summary>
    [Parameter] public Func<Task<IReadOnlyList<TTypeOption>>>? Source { get; set; }

    /// <summary>
    /// Gets or sets the label text displayed above the select element.
    /// When null or empty, no label element is rendered.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Gets or sets whether a non-empty selection is required.
    /// Controls the blank-option text: "Select..." when <c>true</c>, "(none)" when <c>false</c>.
    /// </summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>
    /// Gets or sets whether the picker is disabled (read-only).
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the optional logger. Falls back to <see cref="NullLogger{T}.Instance"/> when not supplied.
    /// </summary>
    // Why: NullLogger fallback is the only acceptable ?? fallback per FDW conventions.
    [Parameter] public ILogger? Logger { get; set; }

    // ── Parameters: search + grouping (new, optional, backward-compatible) ────────

    /// <summary>
    /// Gets or sets whether a type-ahead search input is rendered above the option list.
    /// Filtering is done client-side over the already-loaded option set. Defaults to <c>false</c>.
    /// </summary>
    [Parameter] public bool Searchable { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text for the search input.
    /// Only used when <see cref="Searchable"/> is <c>true</c>.
    /// </summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>
    /// Gets or sets a function that returns the group label for an option.
    /// When provided the picker renders a grouped, collapsible card list (like the
    /// TransformTypeSelector UX) instead of a plain <c>&lt;select&gt;</c>.
    /// </summary>
    [Parameter] public Func<TTypeOption, string>? GroupSelector { get; set; }

    /// <summary>
    /// Gets or sets the per-group option count above which a single group switches to a
    /// <see cref="Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize{TItem}"/> block.
    /// Only applies in grouped mode (when <see cref="GroupSelector"/> is set). Defaults to 50.
    /// </summary>
    [Parameter] public int VirtualizeThreshold { get; set; } = 50;

    // ── Private State ─────────────────────────────────────────────────────────────

    private IReadOnlyList<TTypeOption> _options = [];
    private bool _isLoading;
    private string? _errorMessage;
    private bool _initialized;
    // Why: Track the last StaticOptions reference so we only rebuild _options when the source
    // collection reference changes. Rebuilding on every OnParametersSet call creates a new list
    // object each render cycle; the Blazor interactive circuit sees the @foreach iterate a
    // different list reference mid-batch and throws "error applying batch 2". Stable reference
    // comparison avoids the diff instability without losing reactivity when the caller swaps sources.
    private IEnumerable<TTypeOption>? _lastStaticOptions;
    private string _searchText = string.Empty;
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.OrdinalIgnoreCase);

    // Why: cache filtered results so the template accesses the same list object throughout a
    // single render pass, rather than re-running LINQ on each @if / @foreach reference.
    // Rebuilt whenever _searchText or _options changes (tracked via SetSearchText / LoadFromSource).
    private IReadOnlyList<TTypeOption> _filteredOptions = [];
    private IReadOnlyDictionary<string, List<TTypeOption>> _filteredGroups =
        new Dictionary<string, List<TTypeOption>>(StringComparer.OrdinalIgnoreCase);

    private ILogger ResolvedLogger => Logger ?? NullLogger.Instance;

    // ── Lifecycle ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Why: StaticOptions are cheap synchronous enumerations from TypeCollection.All().
        // Assign directly without an async round-trip. Only rebuild _options when the reference
        // changes — rebuilding every cycle caused "error applying batch 2" on interactive circuits
        // because the @foreach was iterating a fresh list reference on each parameter pass.
        if (StaticOptions is not null && !ReferenceEquals(StaticOptions, _lastStaticOptions))
        {
            _lastStaticOptions = StaticOptions;
            _options = StaticOptions.ToList();
            RebuildFilter();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _initialized || StaticOptions is not null || Source is null)
            return;

        _initialized = true;
        await LoadFromSource();
    }

    // ── Private Methods ───────────────────────────────────────────────────────────

    private async Task LoadFromSource()
    {
        if (Source is null) return;

        _isLoading = true;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            _options = await Source();
            RebuildFilter();
        }
        catch (OperationCanceledException ex)
        {
            // Why: cancellation is expected when the component is disposed; ex is named to satisfy FDW022.
            _ = ex;
            return;
        }
        catch (Exception ex)
        {
            _errorMessage = OptionPickerLog.LoadFailed(ResolvedLogger, ex, typeof(TTypeOption).Name).Message;
            _options = [];
            RebuildFilter();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // Why: called from both OnParametersSet (StaticOptions change) and LoadFromSource.
    // Keeps _filteredOptions and _filteredGroups in sync with the current _options + _searchText
    // without allocating on every render-pass (the template references each exactly once).
    private void RebuildFilter()
    {
        _filteredOptions = string.IsNullOrEmpty(_searchText)
            ? _options
            : _options
                .Where(o => o.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (GroupSelector is not null)
        {
            // Why: capture GroupSelector locally to avoid capturing 'this' in the lambda
            // (Roslyn CS8636 can fire for captured generic type parameters in certain analyzers).
            var selector = GroupSelector;
            _filteredGroups = _filteredOptions
                .GroupBy(o => { var g = selector(o); return string.IsNullOrEmpty(g) ? "Other" : g; }, StringComparer.OrdinalIgnoreCase)
                .OrderBy(grp => grp.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary<IGrouping<string, TTypeOption>, string, List<TTypeOption>>(
                    grp => grp.Key,
                    grp => grp.OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _filteredGroups = new Dictionary<string, List<TTypeOption>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    // Why: @bind:set delivers the select's chosen value as a string (not ChangeEventArgs). Normalise the
    // empty placeholder to null and raise ValueChanged so the parent two-way binding stays in sync.
    private Task HandleChange(string? newValue)
    {
        Value = string.IsNullOrEmpty(newValue) ? null : newValue;
        return ValueChanged.InvokeAsync(Value);
    }

    // Why: grouped view uses button @onclick instead of <select> @bind:set; same normalisation applies.
    private Task HandleGroupedSelect(string optName)
    {
        Value = string.IsNullOrEmpty(optName) ? null : optName;
        return ValueChanged.InvokeAsync(Value);
    }

    private void ToggleGroup(string groupKey)
    {
        if (!_collapsedGroups.Remove(groupKey))
            _collapsedGroups.Add(groupKey);
    }

    // Why: explicit @oninput handler keeps RebuildFilter as the single rebuild site and makes
    // the trigger visible in the code-behind rather than hidden inside a @bind expression.
    private void HandleSearchInput(ChangeEventArgs e)
    {
        var newText = e.Value?.ToString() ?? string.Empty;
        if (string.Equals(_searchText, newText, StringComparison.Ordinal))
            return;
        _searchText = newText;
        RebuildFilter();
    }
}
