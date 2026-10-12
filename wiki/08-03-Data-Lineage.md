# Data Lineage

The `Fdw.Data.Lineage` package provides a directed graph model for tracking data flow between datasets, pipelines, and transforms. The `LineageGraph` class offers BFS traversal and cycle detection.

## Core Types

```
Data.Lineage/
├── LineageGraph.cs          # Graph container + traversal methods
├── LineageNode.cs           # Node (dataset, pipeline, transform)
├── LineageEdge.cs           # Directed edge between nodes
├── LineageNodeTypes.cs      # TypeCollection: Table, View, Pipeline, ...
├── LineageEdgeTypes.cs      # TypeCollection: DerivedFrom, Feeds, Transforms, ...
├── LineageNodeStatuses.cs   # TypeCollection: Active, Deprecated, ...
```

## LineageGraph

A `LineageGraph` holds nodes and directed edges. Edges are directional: `SourceId → TargetId` (source feeds into target).

```csharp
var graph = new LineageGraph
{
    Nodes = new List<LineageNode>
    {
        new() { Id = "raw-orders", Name = "Raw Orders", NodeType = "Table" },
        new() { Id = "clean-orders", Name = "Cleaned Orders", NodeType = "View" },
        new() { Id = "revenue-report", Name = "Revenue Report", NodeType = "Pipeline" }
    },
    Edges = new List<LineageEdge>
    {
        new() { SourceId = "raw-orders", TargetId = "clean-orders", EdgeType = "DerivedFrom" },
        new() { SourceId = "clean-orders", TargetId = "revenue-report", EdgeType = "Feeds" }
    }
};
```

## Traversal Methods (v0.8.1)

All BFS traversal methods are cycle-safe — visited node tracking prevents infinite loops in circular graphs.

### Direct Neighbours

```csharp
// Nodes that feed directly into nodeId (one hop upstream)
IEnumerable<LineageNode> upstream = graph.GetUpstream("revenue-report");
// → [clean-orders]

// Nodes that nodeId feeds into directly (one hop downstream)
IEnumerable<LineageNode> downstream = graph.GetDownstream("raw-orders");
// → [clean-orders]
```

### Full Ancestry / Descendants

```csharp
// All ancestors, BFS, cycle-safe
IReadOnlyList<LineageNode> allUpstream = graph.GetUpstreamAll("revenue-report");
// → [clean-orders, raw-orders]

// All descendants, BFS, cycle-safe
IReadOnlyList<LineageNode> allDownstream = graph.GetDownstreamAll("raw-orders");
// → [clean-orders, revenue-report]
```

Nodes are returned in breadth-first order, deduplicated. The start node itself is excluded from results.

### Impact Analysis

```csharp
// Which nodes are affected if "raw-orders" changes?
IReadOnlyList<LineageNode> affected = graph.GetImpact("raw-orders");
// → [clean-orders, revenue-report]
// GetImpact is an alias for GetDownstreamAll.
```

### Cycle Detection

```csharp
// Detects cycles reachable from a given node (DFS with recursion stack)
bool hasCycle = graph.HasCycle("raw-orders");
// → false (no cycle in this graph)
```

`HasCycle(nodeId)` performs a DFS from the given node and returns `true` if any cycle is reachable from it. To check the whole graph, call it once per root node or per all nodes.

## Method Reference

| Method | Description | Algorithm |
|--------|-------------|-----------|
| `FindNode(id)` | Find a node by ID | Linear scan |
| `GetEdgesForNode(id)` | All edges connected to a node | Linear scan |
| `GetUpstream(id)` | Direct upstream neighbours | Linear scan |
| `GetDownstream(id)` | Direct downstream neighbours | Linear scan |
| `GetUpstreamAll(id)` | All ancestors, BFS, cycle-safe | BFS with visited set |
| `GetDownstreamAll(id)` | All descendants, BFS, cycle-safe | BFS with visited set |
| `GetImpact(id)` | Impact analysis (alias for `GetDownstreamAll`) | BFS with visited set |
| `HasCycle(id)` | True if a cycle exists from this node | DFS with recursion stack |

## TypeCollections

Node types, edge types, and statuses are defined as TypeCollections for O(1) lookup.

```csharp
// Lookup by name
var nodeType = LineageNodeTypes.ByName("Table");   // → TableNodeType instance
var edgeType = LineageEdgeTypes.ByName("Feeds");   // → FeedsEdgeType instance
var status   = LineageNodeStatuses.ByName("Active"); // → ActiveNodeStatus instance
```

Access `.IsEmpty` to check for unknown types rather than null checks:

```csharp
var nodeType = LineageNodeTypes.ByName(unknownName);
if (nodeType.IsEmpty)
{
    return GenericResult.Failure(LineageLog.UnknownNodeType(_logger, unknownName));
}
```

## Usage with Data Lineage Service

Lineage graphs are constructed by a lineage service from the configured pipelines, datasets, and connections (read through the configuration gateway) and returned as a `LineageGraph` object for traversal:

```csharp
public class ReportService(ILineageService lineage)
{
    public async Task<IReadOnlyList<LineageNode>> GetUpstreamSources(string reportId, CancellationToken ct)
    {
        var graphResult = await lineage.GetGraph(reportId, ct);
        if (!graphResult.IsSuccess) return [];

        return graphResult.Value.GetUpstreamAll(reportId);
    }

    public async Task<bool> WillChangeBreakDownstream(string datasetId, CancellationToken ct)
    {
        var graphResult = await lineage.GetGraph(datasetId, ct);
        if (!graphResult.IsSuccess) return false;

        var impacted = graphResult.Value.GetImpact(datasetId);
        return impacted.Count > 0;
    }
}
```

## See Also

- [Schema Abstractions](08-01-Schema-Abstractions.md)
- [Database Schema](08-02-Database-Schema.md)
- [DataGateway Pattern](05-01-DataGateway-Pattern.md)
