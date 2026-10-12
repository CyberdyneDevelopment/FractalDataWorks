using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="NotifyStages"/> (FDW-1120): the Notify stages of a stage tree are found in execution order, however
/// deeply they sit in a Branch, each addressed by the path of names down to it, and a disabled stage hides what is inside it.
/// </summary>
public sealed class NotifyStagesTests
{
    private static PipelineTransformConfiguration Notify(string name, int order, string runPoint, bool enabled = true) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        OperationType = "Notify",
        ExecutionOrder = order,
        IsEnabled = enabled,
        RunPoint = runPoint,
        NotificationRuleName = $"rule-{name}",
        NotificationMessage = $"message-{name}"
    };

    private static PipelineTransformConfiguration Branch(string name, int order, string path, params PipelineTransformConfiguration[] inside) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        OperationType = "Branch",
        ExecutionOrder = order,
        IsEnabled = true,
        Paths = [new PipelineTransformPathConfiguration { Name = path, Condition = "X > 1", Ordinal = 1, Stages = inside }]
    };

    private static PipelineTransformConfiguration Filter(int order) => new()
    {
        Id = Guid.NewGuid(), Name = "keep", OperationType = "Filter", ExecutionOrder = order, IsEnabled = true, FilterExpression = "X > 0"
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void StagesAreFoundInExecutionOrderWhateverOrderTheyAreListedIn()
    {
        // Arrange
        IList<PipelineTransformConfiguration> stages = [Notify("late", 3, "AfterLoad"), Filter(2), Notify("early", 1, "AfterStage")];

        // Act
        var found = NotifyStages.Of(stages);

        // Assert
        found.Select(stage => stage.StageName).ShouldBe(new[] { "early", "late" });
        found.Select(stage => stage.RunPoint).ShouldBe(new[] { "AfterStage", "AfterLoad" });
        found[0].RuleName.ShouldBe("rule-early");
        found[0].Message.ShouldBe("message-early");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStageInsideABranchPathIsFoundWithItsFullPath()
    {
        // Arrange
        IList<PipelineTransformConfiguration> stages = [Branch("route", 1, "big", Notify("tell", 1, "OnFailure"))];

        // Act
        var found = NotifyStages.Of(stages);

        // Assert
        found.Count.ShouldBe(1);
        found[0].StageName.ShouldBe("tell");
        found[0].StagePath.ShouldBe(new[] { "route", "big", "tell" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ABranchInsideABranchIsSearchedToo()
    {
        // Arrange
        IList<PipelineTransformConfiguration> stages = [Branch("outer", 1, "left", Branch("inner", 1, "deep", Notify("tell", 1, "AfterLoad")))];

        // Act
        var found = NotifyStages.Of(stages);

        // Assert
        found.Single().StagePath.ShouldBe(new[] { "outer", "left", "inner", "deep", "tell" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ADisabledStageAndEverythingInsideItAreLeftOut()
    {
        // Arrange
        var disabledBranch = Branch("route", 2, "big", Notify("hidden", 1, "AfterLoad"));
        disabledBranch.IsEnabled = false;
        IList<PipelineTransformConfiguration> stages = [Notify("off", 1, "AfterLoad", enabled: false), disabledBranch, Notify("on", 3, "AfterLoad")];

        // Act
        var found = NotifyStages.Of(stages);

        // Assert
        found.Select(stage => stage.StageName).ShouldBe(new[] { "on" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NoStagesAndNoNotifyStagesAreBothEmpty()
    {
        NotifyStages.Of(null).ShouldBeEmpty();
        NotifyStages.Of([Filter(1)]).ShouldBeEmpty();
    }
}
