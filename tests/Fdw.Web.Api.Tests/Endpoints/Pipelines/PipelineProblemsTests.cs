using System.Linq;
using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="PipelineProblems"/>: how a request-validation failure is placed on the stage and the field
/// of a pipeline draft the Pipeline Builder is showing (FDW-1095).
/// </summary>
public sealed class PipelineProblemsTests
{
    private static CreatePipelineRequest Draft(params string[] stageNames) =>
        new()
        {
            Name = "demo",
            Transforms = [.. stageNames.Select(name => new CreatePipelineTransformRequest { Name = name, OperationType = "Map" })],
        };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FailureInsideANamedStageIsPlacedOnThatStageWithThePathRelativeToIt()
    {
        var problem = PipelineProblems.FromFailure(
            "Transforms[1].FieldMappings[0].SourceField", "Field mapping SourceField is required", Draft("Clean", "Rename"));

        problem.Stage.ShouldBe("Rename");
        problem.Field.ShouldBe("FieldMappings[0].SourceField");
        problem.Reason.ShouldBe("Field mapping SourceField is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FailureInAStageWithNoNameKeepsTheWholePathSoItsPositionIsNotLost()
    {
        var problem = PipelineProblems.FromFailure("Transforms[0].Name", "Transform Name is required", Draft(string.Empty));

        problem.Stage.ShouldBeNull();
        problem.Field.ShouldBe("Transforms[0].Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FailureOutsideAnyStageBelongsToThePipeline()
    {
        var problem = PipelineProblems.FromFailure("TruncateBeforeLoad", "TruncateBeforeLoad disagrees with the load mode", Draft("Clean"));

        problem.Stage.ShouldBeNull();
        problem.Field.ShouldBe("TruncateBeforeLoad");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FailurePointingPastTheLastStageKeepsTheWholePath()
    {
        var problem = PipelineProblems.FromFailure("Transforms[5].Name", "Transform Name is required", Draft("Clean"));

        problem.Stage.ShouldBeNull();
        problem.Field.ShouldBe("Transforms[5].Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void StagePrefixIsMatchedIgnoringCase()
    {
        var problem = PipelineProblems.FromFailure("transforms[0].name", "Transform Name is required", Draft("Clean"));

        problem.Stage.ShouldBe("Clean");
        problem.Field.ShouldBe("name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FailureOnTheStageItselfHasAnEmptyRelativeField()
    {
        var problem = PipelineProblems.FromFailure("Transforms[0]", "Stage is not valid", Draft("Clean"));

        problem.Stage.ShouldBe("Clean");
        problem.Field.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void PipelineProblemHasNoStage()
    {
        var problem = PipelineProblems.OfPipeline("Name", "A pipeline named 'demo' already exists");

        problem.Stage.ShouldBeNull();
        problem.Field.ShouldBe("Name");
        problem.Reason.ShouldBe("A pipeline named 'demo' already exists");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnUpdateThatStatesNoTransformsPlacesEveryFailureOnThePipeline()
    {
        var problem = PipelineProblems.FromFailure("Transforms[0].Name", "Transform Name is required", (IList<CreatePipelineTransformRequest>?)null);

        problem.Stage.ShouldBeNull();
        problem.Field.ShouldBe("Transforms[0].Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AFailureInTheNamedStageOfAnUpdateIsPlacedOnThatStage()
    {
        IList<CreatePipelineTransformRequest> transforms = [new CreatePipelineTransformRequest { Name = "Clean", OperationType = "Map" }];

        var problem = PipelineProblems.FromFailure("Transforms[0].OperationType", "OperationType is required", transforms);

        problem.Stage.ShouldBe("Clean");
        problem.Field.ShouldBe("OperationType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AFailureThatGivesAMessageIsReportedWithThatMessage()
    {
        PipelineProblems.ReasonOf("the roles do not carry datasets:write", "checking the run roles").ShouldBe("the roles do not carry datasets:write");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AFailureThatGivesNoMessageIsReportedAsAServerFaultAndNothingIsInvented()
    {
        var reason = PipelineProblems.ReasonOf(null, "checking the run roles");

        reason.ShouldContain("checking the run roles");
        reason.ShouldContain("see the server log");
    }
}
