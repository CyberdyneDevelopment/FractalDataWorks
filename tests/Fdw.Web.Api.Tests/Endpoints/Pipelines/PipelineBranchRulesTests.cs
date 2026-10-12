using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for the Branch stage on the pipeline API (FDW-1120, contract v5): paths and the stages inside them are mapped by the
/// rules that map a pipeline's own stages, a name is used once across the whole tree, a limit on nesting is read from the stage
/// kind, a refusal carries the path to the stage it is about, and a saved Branch reads back with its paths and stages in order.
/// </summary>
public sealed class PipelineBranchRulesTests
{
    private const char Separator = '\u001F';

    private static CreatePipelineTransformRequest Filter(string name, string expression = "Age >= 18", int order = 1) =>
        new() { Name = name, OperationType = "Filter", FilterExpression = expression, ExecutionOrder = order };

    private static CreatePipelinePathRequest Path(string name, string condition, params CreatePipelineTransformRequest[] stages) =>
        new() { Name = name, Condition = condition, Stages = [.. stages] };

    private static CreatePipelineTransformRequest Branch(string name, string? unmatched, params CreatePipelinePathRequest[] paths) =>
        new() { Name = name, OperationType = "Branch", Unmatched = unmatched, Paths = [.. paths], ExecutionOrder = 1 };

    private static string[] PathOf(Fdw.Results.IGenericResult result) =>
        result.Details!.GetValue<string>("StagePath")!.Split(Separator);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapKeepsPathsInOrderCountedFromOneAndGivesEachPathItsStages()
    {
        // Arrange
        var request = Branch("Route", "Pass",
            Path("Adults", "Age >= 18", Filter("Keep", "Age < 65")),
            Path("Minors", "Age < 18"));

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var stage = result.Value!.Single();
        stage.Unmatched.ShouldBe("Pass");
        stage.Paths.Select(path => (path.Name, path.Condition, path.Ordinal))
            .ShouldBe(new[] { ("Adults", "Age >= 18", 1), ("Minors", "Age < 18", 2) });
        stage.Paths[0].Stages.Select(inner => inner.Name).ShouldBe(new[] { "Keep" });
        stage.Paths[1].Stages.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANameIsUsedOnceAcrossTheWholeTreeIgnoringCase()
    {
        // Arrange: a top-level stage and a stage inside a path share a name
        var request = Branch("Route", "Pass", Path("Adults", "Age >= 18", Filter("keep", "Age < 65")));

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Filter("Keep"), request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Route.Adults.keep");
        PathOf(result).ShouldBe(new[] { "Route", "Adults", "keep" });
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("more than one stage");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARefusalInsideAPathBecomesAProblemOnThatStagePath()
    {
        // Arrange
        var request = Branch("Route", "Pass", Path("Adults", "Age >= 18", Filter("Broken", "Age >")));
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [request], NullLogger.Instance);

        // Act
        var problem = PipelineProblems.OfStageRefusal(result, "mapping the stages");

        // Assert
        problem.Stage.ShouldBe("Route.Adults.Broken");
        problem.StagePath.ShouldBe(new[] { "Route", "Adults", "Broken" });
        problem.Reason.ShouldContain("Age >");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStageThatHasNoPathInItsRefusalIsPlacedOnThePipelinesStages()
    {
        // Arrange: a refusal that carries no stage path
        var result = Fdw.Results.GenericResult<List<PipelineTransformConfiguration>>.Failure(
            Fdw.Services.Etl.Results.EtlResultCodes.ByName("InvalidPipelineStep"),
            NullLogger.Instance,
            Fdw.Results.ResultDetails.Create("Reason", "the stages could not be mapped"));

        // Act
        var problem = PipelineProblems.OfStageRefusal(result, "mapping the stages");

        // Assert
        problem.Stage.ShouldBeNull();
        problem.Field.ShouldBe("Transforms");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void BranchesMayNestToTheLimitTheKindStatesAndNotBeyondIt()
    {
        // Arrange: the limit is read from the Branch kind
        var limit = new BranchTransformType().MaxDepth;
        var nested = Filter("Leaf");
        CreatePipelineTransformRequest branch = nested;
        for (var level = limit; level >= 1; level--)
        {
            branch = Branch($"B{level}", "Pass", Path($"P{level}", "Age >= 18", branch));
        }

        var tooDeep = Branch("Over", "Pass", Path("OverPath", "Age >= 18", branch));

        // Act
        var atLimit = PipelineTransformConfigurationMapper.Map("BatchCopy", [branch], NullLogger.Instance);
        var beyond = PipelineTransformConfigurationMapper.Map("BatchCopy", [tooDeep], NullLogger.Instance);

        // Assert
        atLimit.IsSuccess.ShouldBeTrue(atLimit.CurrentMessage);
        beyond.IsSuccess.ShouldBeFalse();
        beyond.Details!.GetValue<string>("StepName").ShouldBe("Over.OverPath.B1.P1.B2.P2.B3");
        beyond.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain($"more than {limit} deep");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ignore")]
    public void ABranchThatDoesNotSayWhatAnUnmatchedRecordCausesIsRefused(string? unmatched)
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Branch("Route", unmatched, Path("Adults", "Age >= 18"))], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Route");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABranchWithNoPathIsRefused()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Branch("Route", "Pass")], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("at least one path");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("adults.over", "cannot contain '.'")]
    [InlineData("", "every path is named")]
    public void APathNameThatIsEmptyOrHasADotIsRefused(string pathName, string reason)
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Branch("Route", "Pass", Path(pathName, "Age >= 18"))], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain(reason);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoPathsWithTheSameNameAreRefusedIgnoringCase()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy",
            [Branch("Route", "Pass", Path("Adults", "Age >= 18"), Path("adults", "Age < 18"))], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("listed twice");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void APathConditionThatCannotBeReadIsRefusedNamingTheText()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy",
            [Branch("Route", "Pass", Path("Adults", "garbage expression"))], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("garbage expression");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void PathsStatedOnAStageThatIsNotABranchAreRefusedNamingThem()
    {
        // Arrange
        var request = Filter("Keep");
        request.Paths = [Path("Adults", "Age >= 18")];

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("'paths' is not a member of a Filter stage");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ABranchThatStatesAMemberItDoesNotOwnIsRefusedNamingIt()
    {
        // Arrange: a Branch that states a member it does not own, a sort
        var request = Branch("Route", "Pass", Path("Adults", "Age >= 18"));
        request.Sort = [new Fdw.Services.Pipelines.Clients.Abstractions.PipelineSortField { Field = "Age", Direction = "Ascending" }];

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("'sort' is not a member of a Branch stage");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASavedBranchReadsBackItsPathsAndStagesInTheOrderTheyAreTriedAndRun()
    {
        // Arrange: stored out of order, as a read gives them
        var late = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Late", OperationType = "Filter", FilterExpression = "Age < 65", ExecutionOrder = 2 };
        var early = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Early", OperationType = "Filter", FilterExpression = "Age >= 18", ExecutionOrder = 1 };
        var branch = new PipelineTransformConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Route",
            OperationType = "Branch",
            Unmatched = "Reject",
            Paths =
            [
                new PipelineTransformPathConfiguration { Name = "Minors", Condition = "Age < 18", Ordinal = 2 },
                new PipelineTransformPathConfiguration { Name = "Adults", Condition = "Age >= 18", Ordinal = 1, Stages = [late, early] }
            ]
        };

        // Act
        var detail = PipelineDetailMapper.Map(new BatchCopyPipelineConfiguration { Name = "Load", Implementation = "BatchCopy", Transforms = [branch] });

        // Assert
        var stage = detail.Transforms!.Single();
        stage.Unmatched.ShouldBe("Reject");
        stage.Paths.ShouldNotBeNull();
        var paths = stage.Paths!;
        paths.Select(path => path.Name).ShouldBe(new[] { "Adults", "Minors" });
        paths[0].Stages.Select(inner => inner.Name).ShouldBe(new[] { "Early", "Late" });
    }
}
