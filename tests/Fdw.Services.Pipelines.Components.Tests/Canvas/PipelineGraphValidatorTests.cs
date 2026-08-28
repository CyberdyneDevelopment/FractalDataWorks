using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Pipelines.Components.Canvas;
using Fdw.Services.Pipelines.Components.Canvas.Validation;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Abstractions.Canvas.EdgeTypes;
using Fdw.UI.Abstractions.Canvas.NodeTypes;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.RenderModeOptions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Components.Tests.Canvas;

/// <summary>
/// Pure-logic tests for <see cref="PipelineGraphValidator.Validate"/>.
/// Every branch of the validator is exercised.
/// </summary>
public sealed class PipelineGraphValidatorTests
{
    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateSourceToSinkNoTransformsIsValid()
    {
        // Arrange
        var model = PipelineCanvasTestFixtures.BuildValidModel(transformCount: 0);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Issues.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateSourceOneTransformSinkIsValid()
    {
        // Arrange
        var model = PipelineCanvasTestFixtures.BuildValidModel(transformCount: 1);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Issues.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateSourceMultipleTransformsSinkIsValid()
    {
        // Arrange
        var model = PipelineCanvasTestFixtures.BuildValidModel(transformCount: 3);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Issues.ShouldBeEmpty();
    }

    // ── Rule 1: exactly one source ────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateZeroSourcesReturnsError()
    {
        // Arrange — a model with sink only, no source
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode, [sink], []);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeFalse();
        var errors = result.Errors.ToList();
        errors.ShouldContain(e => e.Message.Contains("source DataSet node"));
        errors.ShouldAllBe(e => e.Severity == ValidationSeverities.Error);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateTwoSourcesReturnsError()
    {
        // Arrange
        var source1 = PipelineCanvasTestFixtures.BuildSourceNode("s1", "Src1");
        var source2 = PipelineCanvasTestFixtures.BuildSourceNode("s2", "Src2");
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var edge1 = PipelineCanvasTestFixtures.BuildFlowEdge("s1", "sink");
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode,
            [source1, source2, sink], [edge1]);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Message.Contains("2 source DataSet"));
    }

    // ── Rule 2: exactly one sink ──────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateZeroSinksReturnsError()
    {
        // Arrange — a model with source only, no sink
        var source = PipelineCanvasTestFixtures.BuildSourceNode();
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode, [source], []);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Message.Contains("sink DataSet node"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateTwoSinksReturnsError()
    {
        // Arrange
        var source = PipelineCanvasTestFixtures.BuildSourceNode();
        var sink1 = PipelineCanvasTestFixtures.BuildSinkNode("snk1", "Dest1");
        var sink2 = PipelineCanvasTestFixtures.BuildSinkNode("snk2", "Dest2");
        var edge1 = PipelineCanvasTestFixtures.BuildFlowEdge("source", "snk1");
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode,
            [source, sink1, sink2], [edge1]);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Message.Contains("2 sink DataSet"));
    }

    // ── Rule 3: transform must have OperationType ─────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateTransformMissingOperationTypeReturnsError()
    {
        // Arrange
        var source = PipelineCanvasTestFixtures.BuildSourceNode();
        var transform = PipelineCanvasTestFixtures.BuildTransformNodeNoOperationType("t1");
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var edges = new[]
        {
            PipelineCanvasTestFixtures.BuildFlowEdge("source", "t1"),
            PipelineCanvasTestFixtures.BuildFlowEdge("t1", "sink"),
        };
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode,
            [source, transform, sink], edges);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Message.Contains("OperationType") && e.NodeOrEdgeId == "t1");
    }

    // ── Rule 4: no cycles ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateCycleInFlowEdgesReturnsError()
    {
        // Arrange — t1 → t2 → t1 is a cycle
        var source = PipelineCanvasTestFixtures.BuildSourceNode();
        var t1 = PipelineCanvasTestFixtures.BuildTransformNode("t1", "Map", "T1");
        var t2 = PipelineCanvasTestFixtures.BuildTransformNode("t2", "Filter", "T2", 400);
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var edges = new[]
        {
            PipelineCanvasTestFixtures.BuildFlowEdge("source", "t1"),
            PipelineCanvasTestFixtures.BuildFlowEdge("t1", "t2"),
            PipelineCanvasTestFixtures.BuildFlowEdge("t2", "t1"),  // cycle
            PipelineCanvasTestFixtures.BuildFlowEdge("t2", "sink"),
        };
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode,
            [source, t1, t2, sink], edges);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Message.Contains("cycle"));
    }

    // ── Rule 5: sink reachable from source ────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateDisconnectedSinkReturnsError()
    {
        // Arrange — source and sink present but no edge connecting them
        var source = PipelineCanvasTestFixtures.BuildSourceNode();
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode,
            [source, sink], []);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Message.Contains("not reachable"));
    }

    // ── Rule: orphan nodes produce Warning ────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateOrphanNodeProducesWarning()
    {
        // Arrange — source → sink is connected; t_orphan exists but is not on the path
        var source = PipelineCanvasTestFixtures.BuildSourceNode();
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var orphan = PipelineCanvasTestFixtures.BuildTransformNode("t_orphan", "Map", "Orphan");
        var edges = new[]
        {
            PipelineCanvasTestFixtures.BuildFlowEdge("source", "sink"),
            // orphan has no inbound edge from source and no outbound edge to sink
        };
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode,
            [source, orphan, sink], edges);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Warnings.ShouldContain(w => w.Severity == ValidationSeverities.Warning
                                           && w.NodeOrEdgeId == "t_orphan");
    }

    // ── Empty graph ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateEmptyGraphReturnsErrors()
    {
        // Arrange
        var model = PipelineCanvasTestFixtures.BuildEmptyModel();

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeFalse();
        // Expects at minimum: no-source error + no-sink error
        result.Errors.Count().ShouldBeGreaterThanOrEqualTo(2);
    }

    // ── IsValid reflects errors only ──────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IsValidFalseOnlyWhenErrorsPresent()
    {
        // Arrange — valid topology with one orphan transform → Warning only
        var source = PipelineCanvasTestFixtures.BuildSourceNode();
        var sink = PipelineCanvasTestFixtures.BuildSinkNode();
        var orphan = PipelineCanvasTestFixtures.BuildTransformNode("t_orphan", "Map");
        // orphan is connected from source but not to sink
        var edges = new[]
        {
            PipelineCanvasTestFixtures.BuildFlowEdge("source", "t_orphan"),
            PipelineCanvasTestFixtures.BuildFlowEdge("source", "sink"),
        };
        var model = new PipelineCanvasModel("m1", "Test", PipelineCanvasTestFixtures.EditMode,
            [source, orphan, sink], edges);

        // Act
        var result = PipelineGraphValidator.Validate(model);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.Warnings.ShouldNotBeEmpty();
    }
}
