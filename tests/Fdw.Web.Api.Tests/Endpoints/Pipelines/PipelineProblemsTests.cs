using Fdw.Services.Pipelines.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="PipelineProblems"/>: how a problem is placed on a pipeline and how a failed check's reason is worded (FDW-1095).
/// </summary>
public sealed class PipelineProblemsTests
{
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
