using System.Collections.Generic;
using Fdw.Services.Pipelines.Components.Canvas.Validation;
using Fdw.UI.Abstractions.Components;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Components.Tests.Canvas;

/// <summary>
/// Tests for <see cref="PipelineGraphValidationResult"/>: IsValid, Errors, Warnings filtering.
/// </summary>
public sealed class PipelineGraphValidationResultTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EmptyIssuesIsValid()
    {
        // Arrange
        var result = new PipelineGraphValidationResult([]);

        // Act / Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.Warnings.ShouldBeEmpty();
        result.Issues.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SingleErrorIssueIsNotValid()
    {
        // Arrange
        var issues = new List<PipelineGraphValidationIssue>
        {
            new(ValidationSeverities.Error, "Something went wrong"),
        };
        var result = new PipelineGraphValidationResult(issues);

        // Act / Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem();
        result.Warnings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void WarningOnlyIssuesIsValid()
    {
        // Arrange
        // Why: a Warning-only result should still pass IsValid — warnings don't block execution.
        var issues = new List<PipelineGraphValidationIssue>
        {
            new(ValidationSeverities.Warning, "Orphan node detected"),
        };
        var result = new PipelineGraphValidationResult(issues);

        // Act / Assert
        result.IsValid.ShouldBeTrue();
        result.Warnings.ShouldHaveSingleItem();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MixedIssuesFiltersCorrectly()
    {
        // Arrange
        var issues = new List<PipelineGraphValidationIssue>
        {
            new(ValidationSeverities.Error, "Error one"),
            new(ValidationSeverities.Warning, "Warning one"),
            new(ValidationSeverities.Error, "Error two"),
        };
        var result = new PipelineGraphValidationResult(issues);

        // Act / Assert
        result.IsValid.ShouldBeFalse();
        var errors = new List<PipelineGraphValidationIssue>(result.Errors);
        errors.Count.ShouldBe(2);
        errors.ShouldAllBe(e => e.Severity == ValidationSeverities.Error);

        var warnings = new List<PipelineGraphValidationIssue>(result.Warnings);
        warnings.Count.ShouldBe(1);
        warnings[0].Severity.ShouldBe(ValidationSeverities.Warning);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NodeOrEdgeIdPreservedOnIssue()
    {
        // Arrange
        var nodeId = "transform-abc";
        var issue = new PipelineGraphValidationIssue(ValidationSeverities.Error, "Missing op type", nodeId);
        var result = new PipelineGraphValidationResult([issue]);

        // Act / Assert
        var errors = new List<PipelineGraphValidationIssue>(result.Errors);
        errors[0].NodeOrEdgeId.ShouldBe(nodeId);
        errors[0].Message.ShouldBe("Missing op type");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NullNodeOrEdgeIdIsAllowed()
    {
        // Arrange
        var issue = new PipelineGraphValidationIssue(ValidationSeverities.Error, "Graph-level error");
        var result = new PipelineGraphValidationResult([issue]);

        // Act / Assert
        var errors = new List<PipelineGraphValidationIssue>(result.Errors);
        errors[0].NodeOrEdgeId.ShouldBeNull();
    }
}
