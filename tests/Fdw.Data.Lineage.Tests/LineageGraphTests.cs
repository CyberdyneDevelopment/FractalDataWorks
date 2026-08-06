using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Lineage;
using Shouldly;
using Xunit;

namespace Fdw.Data.Lineage.Tests;

public sealed class LineageGraphTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static LineageNode MakeNode(string id, string name = "") =>
        new() { Id = id, Name = string.IsNullOrEmpty(name) ? id : name };

    private static LineageEdge MakeEdge(string sourceId, string targetId) =>
        new() { Id = $"{sourceId}->{targetId}", SourceId = sourceId, TargetId = targetId };

    /// <summary>
    /// Builds a simple linear chain: A → B → C
    /// </summary>
    private static LineageGraph BuildLinearGraph()
    {
        var graph = new LineageGraph();
        graph.Nodes.Add(MakeNode("A"));
        graph.Nodes.Add(MakeNode("B"));
        graph.Nodes.Add(MakeNode("C"));
        graph.Edges.Add(MakeEdge("A", "B"));
        graph.Edges.Add(MakeEdge("B", "C"));
        return graph;
    }

    /// <summary>
    /// Builds a diamond DAG: A → B, A → C, B → D, C → D
    /// </summary>
    private static LineageGraph BuildDiamondGraph()
    {
        var graph = new LineageGraph();
        graph.Nodes.Add(MakeNode("A"));
        graph.Nodes.Add(MakeNode("B"));
        graph.Nodes.Add(MakeNode("C"));
        graph.Nodes.Add(MakeNode("D"));
        graph.Edges.Add(MakeEdge("A", "B"));
        graph.Edges.Add(MakeEdge("A", "C"));
        graph.Edges.Add(MakeEdge("B", "D"));
        graph.Edges.Add(MakeEdge("C", "D"));
        return graph;
    }

    /// <summary>
    /// Builds a graph with a cycle: A → B → C → A
    /// </summary>
    private static LineageGraph BuildCyclicGraph()
    {
        var graph = new LineageGraph();
        graph.Nodes.Add(MakeNode("A"));
        graph.Nodes.Add(MakeNode("B"));
        graph.Nodes.Add(MakeNode("C"));
        graph.Edges.Add(MakeEdge("A", "B"));
        graph.Edges.Add(MakeEdge("B", "C"));
        graph.Edges.Add(MakeEdge("C", "A")); // closes the cycle
        return graph;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Empty / construction
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyGraphHasNoNodesOrEdges()
    {
        // Arrange & Act
        var graph = LineageGraph.Empty;

        // Assert
        graph.Nodes.ShouldBeEmpty();
        graph.Edges.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyCreatesNewInstanceEachCall()
    {
        // Act
        var a = LineageGraph.Empty;
        var b = LineageGraph.Empty;

        // Assert - each call returns a fresh graph (no shared mutable state)
        a.ShouldNotBeSameAs(b);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // FindNode
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FindNodeReturnsNodeWhenIdExists()
    {
        // Arrange
        var graph = BuildLinearGraph();

        // Act
        var node = graph.FindNode("B");

        // Assert
        node.ShouldNotBeNull();
        node!.Id.ShouldBe("B");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FindNodeReturnsNullWhenIdDoesNotExist()
    {
        // Arrange
        var graph = BuildLinearGraph();

        // Act
        var node = graph.FindNode("Z");

        // Assert
        node.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FindNodeIsCaseSensitive()
    {
        // Arrange
        var graph = new LineageGraph();
        graph.Nodes.Add(MakeNode("myNode"));

        // Act
        var lower = graph.FindNode("myNode");
        var upper = graph.FindNode("MYNODE");

        // Assert
        lower.ShouldNotBeNull();
        upper.ShouldBeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetUpstream / GetDownstream (direct)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetUpstreamReturnsDirectParents()
    {
        // Arrange - B feeds C
        var graph = BuildLinearGraph();

        // Act
        var upstream = graph.GetUpstream("C").ToList();

        // Assert
        upstream.Count.ShouldBe(1);
        upstream[0].Id.ShouldBe("B");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetUpstreamReturnsEmptyForRootNode()
    {
        // Arrange
        var graph = BuildLinearGraph();

        // Act
        var upstream = graph.GetUpstream("A").ToList();

        // Assert
        upstream.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetDownstreamReturnsDirectChildren()
    {
        // Arrange - A feeds B
        var graph = BuildLinearGraph();

        // Act
        var downstream = graph.GetDownstream("A").ToList();

        // Assert
        downstream.Count.ShouldBe(1);
        downstream[0].Id.ShouldBe("B");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetDownstreamReturnsEmptyForLeafNode()
    {
        // Arrange
        var graph = BuildLinearGraph();

        // Act
        var downstream = graph.GetDownstream("C").ToList();

        // Assert
        downstream.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetUpstreamAll — cycle protection
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetUpstreamAllReturnsAllAncestorsWithoutCycles()
    {
        // Arrange - linear A → B → C; from C we expect [B, A]
        var graph = BuildLinearGraph();

        // Act
        var ancestors = graph.GetUpstreamAll("C");

        // Assert
        ancestors.Count.ShouldBe(2);
        ancestors.Select(n => n.Id).ShouldContain("A");
        ancestors.Select(n => n.Id).ShouldContain("B");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetUpstreamAllDoesNotEnterInfiniteLoopOnCyclicGraph()
    {
        // Arrange - A → B → C → A (cycle)
        var graph = BuildCyclicGraph();

        // Act - must terminate without StackOverflowException
        var ancestors = graph.GetUpstreamAll("A");

        // Assert - cycle protection: visited set prevents re-queuing, result is finite
        ancestors.Count.ShouldBeLessThanOrEqualTo(graph.Nodes.Count);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetUpstreamAllExcludesDuplicatesForDiamondGraph()
    {
        // Arrange - diamond: A→B, A→C, B→D, C→D; from D ancestors are [B, C, A]
        var graph = BuildDiamondGraph();

        // Act
        var ancestors = graph.GetUpstreamAll("D");

        // Assert - A appears only once even though it's reachable via B and C
        ancestors.Select(n => n.Id).Distinct().Count().ShouldBe(ancestors.Count);
        ancestors.Select(n => n.Id).ShouldContain("A");
        ancestors.Select(n => n.Id).ShouldContain("B");
        ancestors.Select(n => n.Id).ShouldContain("C");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetUpstreamAllReturnsEmptyForRootNode()
    {
        // Arrange
        var graph = BuildLinearGraph();

        // Act
        var ancestors = graph.GetUpstreamAll("A");

        // Assert
        ancestors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetUpstreamAllReturnsEmptyForIsolatedNode()
    {
        // Arrange
        var graph = new LineageGraph();
        graph.Nodes.Add(MakeNode("Isolated"));

        // Act
        var ancestors = graph.GetUpstreamAll("Isolated");

        // Assert
        ancestors.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetDownstreamAll — cycle protection
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetDownstreamAllReturnsAllDescendantsWithoutCycles()
    {
        // Arrange - linear A → B → C; from A we expect [B, C]
        var graph = BuildLinearGraph();

        // Act
        var descendants = graph.GetDownstreamAll("A");

        // Assert
        descendants.Count.ShouldBe(2);
        descendants.Select(n => n.Id).ShouldContain("B");
        descendants.Select(n => n.Id).ShouldContain("C");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetDownstreamAllDoesNotEnterInfiniteLoopOnCyclicGraph()
    {
        // Arrange - A → B → C → A (cycle)
        var graph = BuildCyclicGraph();

        // Act - must terminate
        var descendants = graph.GetDownstreamAll("A");

        // Assert - cycle protection: result count bounded by node count
        descendants.Count.ShouldBeLessThanOrEqualTo(graph.Nodes.Count);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetDownstreamAllExcludesDuplicatesForDiamondGraph()
    {
        // Arrange - diamond: A→B, A→C, B→D, C→D; from A descendants are [B, C, D]
        var graph = BuildDiamondGraph();

        // Act
        var descendants = graph.GetDownstreamAll("A");

        // Assert - D appears only once even though reachable via B and C
        descendants.Select(n => n.Id).Distinct().Count().ShouldBe(descendants.Count);
        descendants.Select(n => n.Id).ShouldContain("B");
        descendants.Select(n => n.Id).ShouldContain("C");
        descendants.Select(n => n.Id).ShouldContain("D");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetDownstreamAllReturnsEmptyForLeafNode()
    {
        // Arrange
        var graph = BuildLinearGraph();

        // Act
        var descendants = graph.GetDownstreamAll("C");

        // Assert
        descendants.ShouldBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // HasCycle
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void HasCycleReturnsTrueWhenCycleExists()
    {
        // Arrange - A → B → C → A
        var graph = BuildCyclicGraph();

        // Act
        var hasCycle = graph.HasCycle("A");

        // Assert
        hasCycle.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void HasCycleReturnsFalseForLinearDag()
    {
        // Arrange - A → B → C (no cycle)
        var graph = BuildLinearGraph();

        // Act
        var hasCycleFromA = graph.HasCycle("A");
        var hasCycleFromB = graph.HasCycle("B");
        var hasCycleFromC = graph.HasCycle("C");

        // Assert
        hasCycleFromA.ShouldBeFalse();
        hasCycleFromB.ShouldBeFalse();
        hasCycleFromC.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void HasCycleReturnsFalseForDiamondDag()
    {
        // Arrange - diamond with no cycle
        var graph = BuildDiamondGraph();

        // Act & Assert
        graph.HasCycle("A").ShouldBeFalse();
        graph.HasCycle("D").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void HasCycleReturnsTrueForSelfLoop()
    {
        // Arrange - node pointing to itself
        var graph = new LineageGraph();
        graph.Nodes.Add(MakeNode("Self"));
        graph.Edges.Add(MakeEdge("Self", "Self")); // self-loop

        // Act
        var hasCycle = graph.HasCycle("Self");

        // Assert
        hasCycle.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasCycleReturnsFalseForIsolatedNode()
    {
        // Arrange
        var graph = new LineageGraph();
        graph.Nodes.Add(MakeNode("Lone"));

        // Act
        var hasCycle = graph.HasCycle("Lone");

        // Assert
        hasCycle.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void HasCycleDetectsPartialCycleNotReachableFromRoot()
    {
        // Arrange - graph has A → B → C (no cycle from A) and X → Y → X (cycle reachable from X only)
        var graph = BuildLinearGraph();
        graph.Nodes.Add(MakeNode("X"));
        graph.Nodes.Add(MakeNode("Y"));
        graph.Edges.Add(MakeEdge("X", "Y"));
        graph.Edges.Add(MakeEdge("Y", "X")); // cycle between X and Y

        // Act
        var hasCycleFromA = graph.HasCycle("A");
        var hasCycleFromX = graph.HasCycle("X");

        // Assert
        hasCycleFromA.ShouldBeFalse(); // A cannot reach X
        hasCycleFromX.ShouldBeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetImpact
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetImpactReturnsDownstreamNodes()
    {
        // Arrange - A → B → C; if A changes, B and C are impacted
        var graph = BuildLinearGraph();

        // Act
        var impact = graph.GetImpact("A");

        // Assert
        impact.Count.ShouldBe(2);
        impact.Select(n => n.Id).ShouldContain("B");
        impact.Select(n => n.Id).ShouldContain("C");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetImpactReturnsEmptyForLeafNode()
    {
        // Arrange
        var graph = BuildLinearGraph();

        // Act
        var impact = graph.GetImpact("C");

        // Assert
        impact.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetImpactExcludesDuplicatesForDiamondGraph()
    {
        // Arrange - A→B, A→C, B→D, C→D; impact of A should include D only once
        var graph = BuildDiamondGraph();

        // Act
        var impact = graph.GetImpact("A");

        // Assert
        impact.Select(n => n.Id).Distinct().Count().ShouldBe(impact.Count);
        impact.Select(n => n.Id).ShouldContain("D");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetImpactEqualsGetDownstreamAll()
    {
        // Arrange
        var graph = BuildDiamondGraph();

        // Act
        var impact = graph.GetImpact("A");
        var downstream = graph.GetDownstreamAll("A");

        // Assert - GetImpact is defined as GetDownstreamAll
        impact.Select(n => n.Id).OrderBy(x => x)
            .ShouldBe(downstream.Select(n => n.Id).OrderBy(x => x));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetEdgesForNode
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetEdgesForNodeReturnsAllConnectedEdges()
    {
        // Arrange - B is between A and C
        var graph = BuildLinearGraph();

        // Act
        var edges = graph.GetEdgesForNode("B").ToList();

        // Assert - B is both a target (A→B) and source (B→C)
        edges.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetEdgesForNodeReturnsEmptyForNodeWithNoEdges()
    {
        // Arrange
        var graph = new LineageGraph();
        graph.Nodes.Add(MakeNode("Lone"));

        // Act
        var edges = graph.GetEdgesForNode("Lone").ToList();

        // Assert
        edges.ShouldBeEmpty();
    }
}
