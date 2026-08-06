using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Pipelines.Components.Canvas;
using Fdw.Services.Pipelines.Components.Canvas.Projection;
using Fdw.UI.Pipelines.Clients.Models;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Components.Tests.Canvas;

/// <summary>
/// Tests for <see cref="PipelineCanvasProjection.ToCanvas"/>.
/// </summary>
public sealed class PipelineCanvasProjectionToCanvasTests
{
    // ── Task → node mapping ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasMapsTaskToNode()
    {
        // Arrange
        var task = BuildTask(taskType: "Transform", name: "MyTask", x: 150, y: 275);
        var detail = BuildDetail([task]);

        // Act
        var model = PipelineCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        // Assert
        var node = model.Nodes.OfType<PipelineCanvasNode>().Single();
        node.Id.ShouldBe(task.Id.ToString());
        node.Label.ShouldBe("MyTask");
        node.SubLabel.ShouldBe("Transform");
        node.NodeType.Name.ShouldBe("Transform");
        node.X.ShouldBe(150);
        node.Y.ShouldBe(275);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasSkipsTaskWithUnregisteredTaskType()
    {
        // Arrange — an unregistered TaskType is a data/configuration error, not a fabricated node.
        var task = BuildTask(taskType: "NotARealTaskType");
        var detail = BuildDetail([task]);

        // Act
        var model = PipelineCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        // Assert
        model.Nodes.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasConfigurationEntriesBecomeStringMetadata()
    {
        // Arrange — mixed CLR types in Configuration, including an id-shaped (Guid) value.
        var guidValue = Guid.NewGuid();
        var configuration = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ConnectionId"] = guidValue,
            ["BatchSize"] = 250,
            ["Enabled"] = true,
            ["Label"] = "raw",
        };
        var task = BuildTask(configuration: configuration);
        var detail = BuildDetail([task]);

        // Act
        var model = PipelineCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        // Assert
        var node = model.Nodes.OfType<PipelineCanvasNode>().Single();
        node.Metadata["ConnectionId"].ShouldBe(guidValue.ToString());
        node.Metadata["BatchSize"].ShouldBe("250");
        node.Metadata["Enabled"].ShouldBe("True");
        node.Metadata["Label"].ShouldBe("raw");
    }

    // ── Connection → edge mapping ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasMapsConnectionToEdge()
    {
        // Arrange
        var taskA = BuildTask(name: "A");
        var taskB = BuildTask(name: "B");
        var connection = new TaskConnectionPayload
        {
            Id = Guid.NewGuid(),
            SourceTaskId = taskA.Id,
            SourcePort = 0,
            TargetTaskId = taskB.Id,
            TargetPort = 0,
            Label = "to B",
            EdgeKind = "Flow",
        };
        var detail = BuildDetail([taskA, taskB], [connection]);

        // Act
        var model = PipelineCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        // Assert
        model.Edges.Count.ShouldBe(1);
        var edge = model.Edges[0];
        edge.SourceNodeId.ShouldBe(taskA.Id.ToString());
        edge.TargetNodeId.ShouldBe(taskB.Id.ToString());
        edge.EdgeType.Name.ShouldBe("Flow");
        edge.Label.ShouldBe("to B");
        edge.SourcePortId.ShouldBe("out");
        edge.TargetPortId.ShouldBe("in");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasSkipsConnectionWithUnregisteredEdgeKind()
    {
        // Arrange — an unregistered EdgeKind is a data/configuration error, not a fabricated edge.
        var taskA = BuildTask(name: "A");
        var taskB = BuildTask(name: "B");
        var connection = new TaskConnectionPayload
        {
            Id = Guid.NewGuid(),
            SourceTaskId = taskA.Id,
            TargetTaskId = taskB.Id,
            EdgeKind = "NotARealEdgeKind",
        };
        var detail = BuildDetail([taskA, taskB], [connection]);

        // Act
        var model = PipelineCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        // Assert
        model.Nodes.Count.ShouldBe(2);
        model.Edges.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasNonZeroPortIndexPreservedNumerically()
    {
        // Arrange — port index 0 maps to the "in"/"out" convention port; any other index is
        // preserved losslessly as its numeric string (see PipelineCanvasProjection.ToPortId).
        var taskA = BuildTask(name: "A");
        var taskB = BuildTask(name: "B");
        var connection = new TaskConnectionPayload
        {
            Id = Guid.NewGuid(),
            SourceTaskId = taskA.Id,
            SourcePort = 2,
            TargetTaskId = taskB.Id,
            TargetPort = 1,
            EdgeKind = "Flow",
        };
        var detail = BuildDetail([taskA, taskB], [connection]);

        // Act
        var model = PipelineCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        // Assert
        var edge = model.Edges.Single();
        edge.SourcePortId.ShouldBe("2");
        edge.TargetPortId.ShouldBe("1");
    }

    // ── Model wrapper fields ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToCanvasModelIdAndTitleFromDetail()
    {
        // Arrange
        var detail = BuildDetail(name: "My Pipeline");

        // Act
        var model = PipelineCanvasProjection.ToCanvas(detail, PipelineCanvasTestFixtures.EditMode);

        // Assert
        model.Id.ShouldBe(detail.Id.ToString());
        model.Title.ShouldBe("My Pipeline");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static PipelineDetailPayload BuildDetail(
        IList<TaskPayload>? tasks = null,
        IList<TaskConnectionPayload>? connections = null,
        string name = "Test Pipeline")
    {
        return new PipelineDetailPayload
        {
            Id = Guid.NewGuid(),
            Name = name,
            Tasks = tasks ?? new List<TaskPayload>(),
            Connections = connections ?? new List<TaskConnectionPayload>(),
        };
    }

    private static TaskPayload BuildTask(
        string taskType = "Transform",
        string name = "MyTask",
        double x = 100,
        double y = 200,
        IDictionary<string, object?>? configuration = null)
    {
        return new TaskPayload
        {
            Id = Guid.NewGuid(),
            Name = name,
            TaskType = taskType,
            PositionX = x,
            PositionY = y,
            Configuration = configuration ?? new Dictionary<string, object?>(StringComparer.Ordinal),
        };
    }
}

/// <summary>
/// Tests for <see cref="PipelineCanvasProjection.ToDetail"/>.
/// </summary>
public sealed class PipelineCanvasProjectionToDetailTests
{
    // ── Valid graph round trip ─────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToDetailValidGraphMapsNodesToTasks()
    {
        // Arrange
        var model = PipelineCanvasTestFixtures.BuildValidModel(transformCount: 2);
        var canvasNodes = model.Nodes.OfType<PipelineCanvasNode>().ToList();

        // Act
        var result = PipelineCanvasProjection.ToDetail(model);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Tasks.Count.ShouldBe(canvasNodes.Count);

        foreach (var node in canvasNodes)
        {
            var task = result.Value.Tasks.Single(t => string.Equals(t.Name, node.Label, StringComparison.Ordinal));
            task.TaskType.ShouldBe(node.NodeType.Name);
            task.PositionX.ShouldBe(node.X);
            task.PositionY.ShouldBe(node.Y);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToDetailValidGraphMapsEdgesToConnections()
    {
        // Arrange
        var model = PipelineCanvasTestFixtures.BuildValidModel(transformCount: 2);

        // Act
        var result = PipelineCanvasProjection.ToDetail(model);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Connections.Count.ShouldBe(model.Edges.Count);
        result.Value.Connections.ShouldAllBe(c => string.Equals(c.EdgeKind, "Flow", StringComparison.Ordinal));

        var taskIds = result.Value.Tasks.Select(t => t.Id).ToHashSet();
        result.Value.Connections.ShouldAllBe(c => taskIds.Contains(c.SourceTaskId) && taskIds.Contains(c.TargetTaskId));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToDetailMetadataRoundTripsAsStrings()
    {
        // Arrange
        var source = PipelineCanvasTestFixtures.BuildSourceNode();
        var transform = PipelineCanvasTestFixtures.BuildTransformNode("t1", operationType: "Aggregate");
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var edges = new[]
        {
            PipelineCanvasTestFixtures.BuildFlowEdge("source", "t1"),
            PipelineCanvasTestFixtures.BuildFlowEdge("t1", "sink"),
        };
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode,
            [source, transform, sink], edges);

        // Act
        var result = PipelineCanvasProjection.ToDetail(model);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var transformTask = result.Value!.Tasks.Single(t => string.Equals(t.Name, "Transform", StringComparison.Ordinal));
        transformTask.Configuration[PipelineCanvasMetadataKeys.OperationType].ShouldBe("Aggregate");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ToDetailNonGuidModelIdMapsToEmptyGuid()
    {
        // Arrange — fixture model ids ("pipe-1", "m1", etc.) are not Guid-formatted; Guid.Empty is
        // the established "not yet assigned" sentinel (mirrors PipelineBuilderProvider.PipelineConfigurationId).
        var model = PipelineCanvasTestFixtures.BuildValidModel();

        // Act
        var result = PipelineCanvasProjection.ToDetail(model);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Id.ShouldBe(Guid.Empty);
    }

    // ── Invalid graph returns Failure ──────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToDetailInvalidGraphReturnsFailure()
    {
        // Arrange — empty model has no source/sink
        var model = PipelineCanvasTestFixtures.BuildEmptyModel();

        // Act
        var result = PipelineCanvasProjection.ToDetail(model);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToDetailMissingSourceReturnsFailure()
    {
        // Arrange — sink only, no source
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode, [sink], []);

        // Act
        var result = PipelineCanvasProjection.ToDetail(model);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToDetailDisconnectedSinkReturnsFailure()
    {
        // Arrange — source and sink exist but no Flow edge connects them
        var source = PipelineCanvasTestFixtures.BuildSourceNode();
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode,
            [source, sink], []);

        // Act
        var result = PipelineCanvasProjection.ToDetail(model);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ToDetailTransformMissingOperationTypeReturnsFailure()
    {
        // Arrange
        var source = PipelineCanvasTestFixtures.BuildSourceNode();
        var badTransform = PipelineCanvasTestFixtures.BuildTransformNodeNoOperationType("t1");
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var edges = new[]
        {
            PipelineCanvasTestFixtures.BuildFlowEdge("source", "t1"),
            PipelineCanvasTestFixtures.BuildFlowEdge("t1", "sink"),
        };
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode,
            [source, badTransform, sink], edges);

        // Act
        var result = PipelineCanvasProjection.ToDetail(model);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}
