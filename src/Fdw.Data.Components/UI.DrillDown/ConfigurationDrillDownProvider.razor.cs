using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.UI.Providers;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Clients;
using Fdw.Operations.Clients.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.DrillDown;

/// <summary>
/// Headless provider that discovers tree structure from configuration metadata
/// and walks a pre-loaded root data object using reflection to build nodes automatically.
/// </summary>
/// <remarks>
/// <para>
/// The domain provider (e.g., DataStoreDetailProvider) loads the root data object from its
/// own detail endpoint and passes it as <see cref="RootData"/>. This provider then:
/// </para>
/// <list type="number">
///   <item><description>Calls <c>GetChildTypes(parentTableName)</c> recursively to discover the tree shape.</description></item>
///   <item><description>Walks <see cref="RootData"/> using <c>ParentCollectionProperty</c> from metadata to extract children at each level.</description></item>
///   <item><description>Builds <see cref="DrillDownNode{T}"/> tree nodes automatically.</description></item>
///   <item><description>On node selection, populates <c>SelectedNodeProperties</c> and <c>SelectedNodeValuesFrom</c>.</description></item>
/// </list>
/// <para>Headless chain:</para>
/// <list type="bullet">
///   <item><description>Consumer (before): page shells with markup and ctx.OnXxx() calls</description></item>
///   <item><description>This (provider): <see cref="ConfigurationDrillDownProvider"/> — owns metadata discovery + tree walking</description></item>
///   <item><description>Context (after): <see cref="ConfigurationDrillDownContext"/> — sealed; passed to consumer RenderFragment</description></item>
/// </list>
/// </remarks>
public partial class ConfigurationDrillDownProvider : ComponentBase
{
    /// <summary>Gets or sets the pre-loaded root data object from the domain provider.</summary>
    [Parameter] public object? RootData { get; set; }

    /// <summary>Gets or sets the service category (e.g., "DataStore", "Connection").</summary>
    [Parameter] public string ServiceCategory { get; set; } = string.Empty;

    /// <summary>Gets or sets the configuration instance name.</summary>
    [Parameter] public string InstanceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the root table name used to discover child types from metadata.</summary>
    [Parameter] public string RootTableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the child content render fragment receiving the configuration drill-down context.</summary>
    [Parameter] public RenderFragment<ConfigurationDrillDownContext>? ChildContent { get; set; }

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private ILoggerFactory? LoggerFactoryParam { get; set; }
    [Inject] private ILogger<ConfigurationDrillDownProvider>? LoggerParam { get; set; }

    private ILogger<ConfigurationDrillDownProvider> _logger = NullLogger<ConfigurationDrillDownProvider>.Instance;
    private ConfigurationApiClient? _configApi;

    // ── State ──────────────────────────────────────────────────────────────────

    private List<DrillDownNode<object>> _nodes = [];
    private DrillDownNode<object>? _selectedNode;
    private List<DrillDownNode<object>> _breadcrumbPath = [];
    private bool _isLoading;
    private IGenericResult? _lastResult;
    private ConfigurationDrillDownContext _context = new();
    private object? _previousRootData;
    private bool _metadataLoaded;

    // Metadata: table name → child type summaries
    private readonly Dictionary<string, IReadOnlyList<ConfigurationTypeSummary>> _childTypeCache = new(StringComparer.Ordinal);

    // Metadata: table name → collection property name on parent
    private readonly Dictionary<string, string> _collectionPropertyMap = new(StringComparer.Ordinal);

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _logger = LoggerParam ?? NullLogger<ConfigurationDrillDownProvider>.Instance;
        var loggerFactory = LoggerFactoryParam ?? NullLoggerFactory.Instance;

        _configApi = new ConfigurationApiClient(
            HttpClientFactory.CreateClient("ConfigurationClient"),
            loggerFactory.CreateLogger<ConfigurationApiClient>());

        RebuildContext();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!ReferenceEquals(RootData, _previousRootData))
        {
            _previousRootData = RootData;

            if (RootData is not null && !_metadataLoaded)
            {
                await LoadMetadataAndBuildTree();
            }
            else if (RootData is not null)
            {
                RebuildTree();
            }
            else
            {
                _nodes = [];
                _selectedNode = null;
                _breadcrumbPath = [];
                RebuildContext();
            }
        }
    }

    // ── Metadata Loading ──────────────────────────────────────────────────────

    private async Task LoadMetadataAndBuildTree(CancellationToken cancellationToken = default)
    {
        _isLoading = true;
        _lastResult = null;
        RebuildContext();

        try
        {
            ConfigurationDrillDownProviderLog.MetadataLoading(_logger, ServiceCategory);
            await DiscoverChildTypesRecursive(RootTableName, cancellationToken);
            _metadataLoaded = true;

            var totalTypes = _childTypeCache.Values.Sum(list => list.Count);
            ConfigurationDrillDownProviderLog.MetadataLoaded(_logger, totalTypes);

            RebuildTree();
            ConfigurationDrillDownProviderLog.InstanceLoaded(_logger, InstanceName);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return;
        }
        catch (Exception ex)
        {
            _lastResult = GenericResult.Failure(
                new[] { ConfigurationDrillDownProviderLog.MetadataLoadFailed(_logger, ex, ServiceCategory) }
                    .Concat(ExceptionResultExtensions.FlattenException(ex)));
            _isLoading = false;
            RebuildContext();
        }
    }

    private async Task DiscoverChildTypesRecursive(string parentTableName, CancellationToken cancellationToken)
    {
        if (_childTypeCache.ContainsKey(parentTableName))
        {
            return;
        }

        var result = await _configApi!.GetChildTypes(parentTableName, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            _childTypeCache[parentTableName] = [];
            return;
        }

        _childTypeCache[parentTableName] = result.Value;

        // Extract ParentCollectionProperty from the type metadata.
        // The ConfigurationTypeSummary doesn't carry ParentCollectionProperty directly,
        // so we derive it from the TypeName — convention: plural of the child type name.
        // For example, DataPath → "Paths", DataContainer → "Containers", DataContainerField → "Fields"
        foreach (var childType in result.Value)
        {
            // Store the collection property name for this child type.
            // Convention: remove "DataStore"/"Data" prefix, pluralize.
            // This is populated from metadata when available; fallback to convention.
            var collectionProperty = DeriveCollectionPropertyName(childType.TypeName);
            _collectionPropertyMap[childType.TypeName] = collectionProperty;

            // Recurse to discover grandchildren
            await DiscoverChildTypesRecursive(childType.TypeName, cancellationToken);
        }
    }

    // ── Tree Building ──────────────────────────────────────────────────────────

    private void RebuildTree()
    {
        if (RootData is null)
        {
            _nodes = [];
            _selectedNode = null;
            _breadcrumbPath = [];
            _isLoading = false;
            RebuildContext();
            return;
        }

        _isLoading = true;
        _lastResult = null;
        RebuildContext();

        try
        {
            ConfigurationDrillDownProviderLog.TreeBuilding(_logger, InstanceName);

            _childTypeCache.TryGetValue(RootTableName, out var rootChildTypes);
            var childTypes = rootChildTypes ?? (IReadOnlyList<ConfigurationTypeSummary>)[];

            var nodes = new List<DrillDownNode<object>>();

            foreach (var childType in childTypes)
            {
                var collectionProperty = _collectionPropertyMap.GetValueOrDefault(childType.TypeName, string.Empty);
                var childNodes = BuildNodesFromProperty(RootData, collectionProperty, childType, 0);
                nodes.AddRange(childNodes);
            }

            _nodes = nodes;
            _selectedNode = null;
            _breadcrumbPath = [];

            if (_nodes.Count == 0)
            {
                ConfigurationDrillDownProviderLog.EmptyChildren(_logger, RootTableName, InstanceName);
            }
            else
            {
                ConfigurationDrillDownProviderLog.TreeBuilt(_logger, _nodes.Count);
            }
        }
        catch (Exception ex)
        {
            _lastResult = GenericResult.Failure(
                new[] { ConfigurationDrillDownProviderLog.TreeBuildFailed(_logger, ex, InstanceName) }
                    .Concat(ExceptionResultExtensions.FlattenException(ex)));
            _nodes = [];
        }
        finally
        {
            _isLoading = false;
            RebuildContext();
        }
    }

    private List<DrillDownNode<object>> BuildNodesFromProperty(
        object parentData,
        string collectionPropertyName,
        ConfigurationTypeSummary childType,
        int depth)
    {
        if (string.IsNullOrEmpty(collectionPropertyName))
        {
            ConfigurationDrillDownProviderLog.PropertyNotFound(_logger, "(empty)", parentData.GetType().Name);
            return [];
        }

        var property = parentData.GetType().GetProperty(collectionPropertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
        {
            ConfigurationDrillDownProviderLog.PropertyNotFound(_logger, collectionPropertyName, parentData.GetType().Name);
            return [];
        }

        var collectionValue = property.GetValue(parentData);
        if (collectionValue is not IEnumerable enumerable)
        {
            ConfigurationDrillDownProviderLog.EmptyChildren(_logger, collectionPropertyName, childType.DisplayName);
            return [];
        }

        var nodes = new List<DrillDownNode<object>>();

        // Discover grandchild types for this level
        _childTypeCache.TryGetValue(childType.TypeName, out var grandchildTypes);
        var hasGrandchildren = grandchildTypes is not null && grandchildTypes.Count > 0;

        foreach (var item in enumerable)
        {
            if (item is null)
            {
                continue;
            }

            var label = ExtractLabel(item);
            var subtitle = ExtractSubtitle(item, childType);

            var childNodes = new List<DrillDownNode<object>>();

            if (hasGrandchildren)
            {
                foreach (var grandchildType in grandchildTypes!)
                {
                    var grandchildProperty = _collectionPropertyMap.GetValueOrDefault(grandchildType.TypeName, string.Empty);
                    var grandNodes = BuildNodesFromProperty(item, grandchildProperty, grandchildType, depth + 1);
                    childNodes.AddRange(grandNodes);
                }
            }

            nodes.Add(new DrillDownNode<object>
            {
                Item = item,
                Label = label,
                Subtitle = subtitle,
                NodeType = childType.DisplayName,
                Depth = depth,
                IsLeaf = childNodes.Count == 0,
                Children = childNodes
            });
        }

        return nodes;
    }

    // ── Node Selection ─────────────────────────────────────────────────────────

    private void SelectNode(DrillDownNode<object> node)
    {
        if (_selectedNode is not null)
        {
            _selectedNode.IsSelected = false;
        }

        node.IsSelected = true;
        _selectedNode = node;

        ConfigurationDrillDownProviderLog.NodeSelected(_logger, node.Label, node.NodeType);

        _breadcrumbPath = ComputeBreadcrumbPath(node);
        RebuildContext();
        StateHasChanged();
    }

    // ── Toggle Expand/Collapse ─────────────────────────────────────────────────

    private void ToggleExpand(DrillDownNode<object> node)
    {
        node.IsExpanded = !node.IsExpanded;

        if (node.IsExpanded)
        {
            ConfigurationDrillDownProviderLog.NodeExpanded(_logger, node.Label);
        }
        else
        {
            ConfigurationDrillDownProviderLog.NodeCollapsed(_logger, node.Label);
        }

        RebuildContext();
        StateHasChanged();
    }

    // ── Refresh ────────────────────────────────────────────────────────────────

    private async Task<IGenericResult> Refresh()
    {
        try
        {
            _metadataLoaded = false;
            _childTypeCache.Clear();
            _collectionPropertyMap.Clear();
            await LoadMetadataAndBuildTree();
            ConfigurationDrillDownProviderLog.RefreshCompleted(_logger, InstanceName);
        }
        catch (Exception ex)
        {
            RebuildContext();
            return GenericResult.Failure(
                new[] { ConfigurationDrillDownProviderLog.TreeBuildFailed(_logger, ex, InstanceName) }
                    .Concat(ExceptionResultExtensions.FlattenException(ex)));
        }

        // Why: LoadMetadataAndBuildTree records its own failure in _lastResult without throwing, so
        // the refresh reports what the load actually produced rather than an unconditional success.
        return _lastResult ?? GenericResult.Success();
    }

    // ── Dropdown Values ────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<string>> LoadDropdownValues(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _configApi!.GetTypeCollectionValues(collectionName, cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                var values = result.Value.Select(v => v.Name).ToList();
                ConfigurationDrillDownProviderLog.DropdownValuesLoaded(_logger, values.Count, collectionName);
                return values;
            }

            return [];
        }
        catch (Exception ex)
        {
            ConfigurationDrillDownProviderLog.DropdownLoadFailed(_logger, ex, collectionName);
            return [];
        }
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

    // ── Property Extraction ────────────────────────────────────────────────────

    private static string ExtractLabel(object item)
    {
        // Try "Name" then "DisplayName" then fallback to ToString
        var nameProperty = item.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
        if (nameProperty is not null)
        {
            var value = nameProperty.GetValue(item);
            if (value is string s && !string.IsNullOrEmpty(s))
            {
                return s;
            }
        }

        var displayNameProperty = item.GetType().GetProperty("DisplayName", BindingFlags.Public | BindingFlags.Instance);
        if (displayNameProperty is not null)
        {
            var value = displayNameProperty.GetValue(item);
            if (value is string s && !string.IsNullOrEmpty(s))
            {
                return s;
            }
        }

        return item.ToString() ?? "(unknown)";
    }

    private static string? ExtractSubtitle(object item, ConfigurationTypeSummary typeMetadata)
    {
        // Try common subtitle properties based on type conventions
        var typeProperty = item.GetType().GetProperty("PhysicalPath", BindingFlags.Public | BindingFlags.Instance)
            ?? item.GetType().GetProperty("ContainerType", BindingFlags.Public | BindingFlags.Instance)
            ?? item.GetType().GetProperty("DataType", BindingFlags.Public | BindingFlags.Instance)
            ?? item.GetType().GetProperty("PathType", BindingFlags.Public | BindingFlags.Instance);

        if (typeProperty is not null)
        {
            var value = typeProperty.GetValue(item);
            if (value is not null)
            {
                return value.ToString();
            }
        }

        return null;
    }

    private static Dictionary<string, object?> ExtractProperties(object item)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        var type = item.GetType();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Skip collection properties (they represent children, not scalar values)
            if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
            {
                continue;
            }

            properties[prop.Name] = prop.GetValue(item);
        }

        return properties;
    }

    /// <summary>
    /// Derives the collection property name from a child type name using naming conventions.
    /// For example: "DataPath" → "Paths", "DataContainer" → "Containers", "DataContainerField" → "Fields".
    /// </summary>
    private static string DeriveCollectionPropertyName(string childTypeName)
    {
        // Common suffixes that map to pluralized property names
        // DataPath → Paths, DataContainer → Containers, DataContainerField → Fields
        // Connection → Connections, etc.

        // Find the last capitalized word segment
        var lastUpperIndex = -1;
        for (var i = childTypeName.Length - 1; i >= 1; i--)
        {
            if (char.IsUpper(childTypeName[i]))
            {
                lastUpperIndex = i;
                break;
            }
        }

        if (lastUpperIndex > 0)
        {
            var lastWord = childTypeName.Substring(lastUpperIndex);
            return lastWord + "s";
        }

        return childTypeName + "s";
    }

    // ── Context Builder ────────────────────────────────────────────────────────

    private void RebuildContext()
    {
        // Determine metadata and properties for the selected node
        ConfigurationTypeSummary? selectedMetadata = null;
        IReadOnlyList<RelatedCollectionRef> valuesFrom = [];
        IDictionary<string, object?>? selectedProperties = null;

        if (_selectedNode is not null)
        {
            // Find the type summary that matches the selected node's NodeType
            foreach (var kvp in _childTypeCache)
            {
                foreach (var typeSummary in kvp.Value)
                {
                    if (string.Equals(typeSummary.DisplayName, _selectedNode.NodeType, StringComparison.Ordinal))
                    {
                        selectedMetadata = typeSummary;
                        valuesFrom = typeSummary.RelatedCollections;
                        break;
                    }
                }

                if (selectedMetadata is not null)
                {
                    break;
                }
            }

            selectedProperties = ExtractProperties(_selectedNode.Item);
        }

        _context = new ConfigurationDrillDownContext
        {
            IsLoading = _isLoading,
            LastResult = _lastResult,
            Nodes = _nodes,
            SelectedNode = _selectedNode,
            BreadcrumbPath = _breadcrumbPath,
            SelectedTypeMetadata = selectedMetadata,
            SelectedNodeValuesFrom = valuesFrom,
            SelectedNodeProperties = selectedProperties,
            InstanceName = InstanceName,
            ServiceCategory = ServiceCategory,
            OnNodeSelected = SelectNode,
            OnToggleExpand = ToggleExpand,
            OnRefresh = Refresh,
            OnLoadDropdownValues = name => LoadDropdownValues(name)
        };

        ConfigurationDrillDownProviderLog.ContextRebuilt(_logger);
    }
}
