using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Flow;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for a for each that works through its list in batches and a container source that reads its list of files from the variable the round
/// holds (FDW-3086, FDW-3194): what a saved graph may state, what builds, and when a list of files is read.
/// </summary>
public sealed class ForEachBatchGraphTests
{
    private static PipelineComponentConfiguration Each(int? batchSize) => Placed(
        new PipelineComponentConfiguration { Name = "each", ComponentType = "ForEach", VariableName = "Batch", ListVariable = "Names", BatchSize = batchSize }, 1);

    private static PipelineComponentConfiguration Files(string? filesList) => new()
    {
        Name = "files",
        ComponentType = "ContainerSource",
        DataStoreName = "Landing",
        PathName = "inbound",
        ContainerName = "Orders",
        BatchSize = 50,
        TargetKind = "Files",
        FilesList = filesList,
    };

    private static GraphPipelineConfiguration Batched(PipelineComponentConfiguration each, PipelineComponentConfiguration files)
    {
        var graph = Steps(each, DataFlow("inner", [files], [], [], 1, "each"));
        graph.Parameters.Add(new PipelineParameterConfiguration { Name = "Names", ParameterType = "String", IsList = true });
        return graph;
    }

    private static string[] Reasons(GraphPipelineConfiguration graph, string text) =>
        [.. GraphComponentValidator.Problems(graph, [], NullLogger.Instance).Select(problem => problem.Reason).Where(reason => reason.Contains(text))];

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("Batch")]
    [InlineData("Names")]
    public void AListOfFilesMayNameTheRoundVariableOrAListParameter(string filesList) =>
        Reasons(Batched(Each(2), Files(filesList)), "list of files").ShouldBeEmpty();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AListOfFilesNamingNothingInScopeIsRefusedNamingIt()
    {
        var found = Reasons(Batched(Each(2), Files("Nope")), "list of files");

        found.Length.ShouldBe(1);
        found[0].ShouldContain("Nope");
        found[0].ShouldContain("not a run parameter or a variable in scope");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(0)]
    [InlineData(-2)]
    public void ABatchSizeBelowOneIsRefusedWhenTheGraphIsSaved(int batchSize) =>
        Reasons(Batched(Each(batchSize), Files("Batch")), "items a round").ShouldHaveSingleItem().ShouldContain($"takes {batchSize} items a round");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ABatchSizeThatIsNotStatedOrIsStatedWellOrIsBoundToAParameterIsNotRefused()
    {
        var bound = Each(null);
        bound.SettingBindings.Add(new PipelineComponentSettingBindingConfiguration { SettingName = "batchSize", ParameterName = "BatchSize" });

        Reasons(Batched(Each(null), Files("Batch")), "items a round").ShouldBeEmpty();
        Reasons(Batched(Each(20), Files("Batch")), "items a round").ShouldBeEmpty();
        Reasons(Batched(bound, Files("Batch")), "items a round").ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheForEachStatesItsBatchSizeInTheCatalogueAndTheBuilderTakesItFromTheComponent()
    {
        ComponentTypes.ByName("ForEach").Settings.Single(setting => setting.Name == "batchSize").Kind.ShouldBe("Number");
        var context = ContextOn(new GraphPipelineConfiguration { Name = "Ingest" }, new Mock<IDataGateway>().Object);
        IReadOnlyList<IFlowStep> body = [new ScriptedStep("n", _ => Task.FromResult<IGenericResult>(GenericResult.Success()))];

        var built = new ForEachFlowStepBuilder().Create(Each(3), body, context);
        var refused = new ForEachFlowStepBuilder().Create(Each(0), body, context);

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("each");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AListOfFilesThatIsNotARunParameterIsBuiltWhenItsStageStartsAndOneThatIsAParameterIsBuiltAtOnce()
    {
        var context = ContextWith(new GraphPipelineConfiguration { Name = "Ingest" }, new Dictionary<string, object?> { ["Names"] = new List<string> { "a.csv" } });

        GraphFlowBuilder.ReadsFilesFromVariable(Files("Batch"), context).ShouldBeTrue();
        GraphFlowBuilder.ReadsFilesFromVariable(Files("Names"), context).ShouldBeFalse();
        GraphFlowBuilder.ReadsFilesFromVariable(Files(null), context).ShouldBeFalse();
    }
}
