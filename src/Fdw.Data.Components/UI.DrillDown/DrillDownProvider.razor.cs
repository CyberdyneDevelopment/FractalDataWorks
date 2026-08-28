using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.UI.Providers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.DrillDown;

/// <summary>
/// Headless provider for recursive drill-down navigation. Owns tree building,
/// expand/collapse state, node selection, and breadcrumb computation. No UI markup.
/// </summary>
/// <typeparam name="T">The type of the root data object used to build the tree.</typeparam>
/// <remarks>
/// Headless chain:
/// <list type="bullet">
///   <item><description>Consumer (before): page shells with markup and ctx.OnXxx() calls</description></item>
///   <item><description>This (provider): <see cref="DrillDownProvider{T}"/> — owns all state and logic, exposes via <see cref="DrillDownContext{T}"/></description></item>
///   <item><description>Context (after): <see cref="DrillDownContext{T}"/> — sealed; passed to consumer RenderFragment</description></item>
/// </list>
/// </remarks>
public partial class DrillDownProvider<T> : ComponentBase
{
    /// <summary>Gets or sets the root data object from which the tree is built.</summary>
    [Parameter] public T? Root { get; set; }

    /// <summary>Gets or sets the delegate that transforms the root object into a list of tree nodes.</summary>
    [Parameter] public Func<T, IReadOnlyList<DrillDownNode<object>>>? BuildTree { get; set; }

    /// <summary>Gets or sets the child content render fragment receiving the drill-down context.</summary>
    [Parameter] public RenderFragment<DrillDownContext<T>>? ChildContent { get; set; }

    [Inject] private ILogger<DrillDownProvider<T>>? LoggerParam { get; set; }

    private ILogger<DrillDownProvider<T>> _logger = NullLogger<DrillDownProvider<T>>.Instance;

    // ── State ──────────────────────────────────────────────────────────────────

    private IReadOnlyList<DrillDownNode<object>> _nodes = [];
    private DrillDownNode<object>? _selectedNode;
    private List<DrillDownNode<object>> _breadcrumbPath = [];
    private bool _isLoading;
    private IGenericResult? _lastResult;
    private DrillDownContext<T> _context = new();
    private T? _previousRoot;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _logger = LoggerParam ?? NullLogger<DrillDownProvider<T>>.Instance;
        RebuildContext();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Equals(Root, _previousRoot))
        {
            _previousRoot = Root;
            RebuildTree();
        }
    }

    // ── Tree Building ──────────────────────────────────────────────────────────

    private void RebuildTree()
    {
        if (Root is null || BuildTree is null)
        {
            _nodes = [];
            _selectedNode = null;
            _breadcrumbPath = [];
            RebuildContext();
            return;
        }

        _isLoading = true;
        _lastResult = null;
        RebuildContext();

        try
        {
            var result = BuildTree(Root);
            if (result is null)
            {
                DrillDownProviderLog.BuildTreeReturnedNull(_logger);
                _nodes = [];
            }
            else
            {
                _nodes = result;
                if (_nodes.Count == 0)
                {
                    DrillDownProviderLog.EmptyTree(_logger);
                }
                else
                {
                    DrillDownProviderLog.TreeBuilt(_logger, _nodes.Count);
                }
            }

            DrillDownProviderLog.TreeRebuilt(_logger);
            DrillDownProviderLog.RootLoaded(_logger);

            // Reset selection when tree changes
            _selectedNode = null;
            _breadcrumbPath = [];
        }
        catch (Exception ex)
        {
            _lastResult = GenericResult.Failure(
                new[] { DrillDownProviderLog.BuildTreeFailed(_logger, ex) }
                    .Concat(ExceptionResultExtensions.FlattenException(ex)));
            _nodes = [];
        }
        finally
        {
            _isLoading = false;
            RebuildContext();
        }
    }

    // ── Node Selection ─────────────────────────────────────────────────────────

    private void SelectNode(DrillDownNode<object> node)
    {
        // Deselect previous
        if (_selectedNode is not null)
        {
            _selectedNode.IsSelected = false;
        }

        node.IsSelected = true;
        _selectedNode = node;

        DrillDownProviderLog.NodeSelected(_logger, node.Label, node.NodeType);

        // Compute breadcrumb path
        _breadcrumbPath = ComputeBreadcrumbPath(node);
        DrillDownProviderLog.BreadcrumbComputed(_logger, _breadcrumbPath.Count);

        RebuildContext();
        StateHasChanged();
    }

    // ── Toggle Expand/Collapse ─────────────────────────────────────────────────

    private void ToggleExpand(DrillDownNode<object> node)
    {
        node.IsExpanded = !node.IsExpanded;

        if (node.IsExpanded)
        {
            DrillDownProviderLog.NodeExpanded(_logger, node.Label);
        }
        else
        {
            DrillDownProviderLog.NodeCollapsed(_logger, node.Label);
        }

        RebuildContext();
        StateHasChanged();
    }

    // ── Refresh ────────────────────────────────────────────────────────────────

    private Task<IGenericResult> Refresh() => Task.FromResult(RefreshCore());

    private IGenericResult RefreshCore()
    {
        try
        {
            RebuildTree();
            DrillDownProviderLog.RefreshCompleted(_logger);
        }
        catch (Exception ex)
        {
            RebuildContext();
            return GenericResult.Failure(
                new[] { DrillDownProviderLog.RefreshFailed(_logger, ex) }
                    .Concat(ExceptionResultExtensions.FlattenException(ex)));
        }

        return _lastResult ?? GenericResult.Success();
    }

    // ── Breadcrumb Computation ─────────────────────────────────────────────────

    private List<DrillDownNode<object>> ComputeBreadcrumbPath(DrillDownNode<object> target)
    {
        var path = new List<DrillDownNode<object>>();
        if (FindPathToNode(_nodes, target, path))
        {
            return path;
        }

        return [target];
    }

    private static bool FindPathToNode(
        IReadOnlyList<DrillDownNode<object>> nodes,
        DrillDownNode<object> target,
        List<DrillDownNode<object>> path)
    {
        foreach (var node in nodes)
        {
            path.Add(node);

            if (ReferenceEquals(node, target))
            {
                return true;
            }

            if (node.Children.Count > 0 && FindPathToNode(node.Children, target, path))
            {
                return true;
            }

            path.RemoveAt(path.Count - 1);
        }

        return false;
    }

    // ── Context Builder ────────────────────────────────────────────────────────

    private void RebuildContext()
    {
        _context = new DrillDownContext<T>
        {
            Root = Root,
            IsLoading = _isLoading,
            LastResult = _lastResult,
            Nodes = _nodes,
            SelectedNode = _selectedNode,
            BreadcrumbPath = _breadcrumbPath,
            OnNodeSelected = SelectNode,
            OnToggleExpand = ToggleExpand,
            OnRefresh = Refresh
        };

        DrillDownProviderLog.ContextRebuilt(_logger);
    }
}
