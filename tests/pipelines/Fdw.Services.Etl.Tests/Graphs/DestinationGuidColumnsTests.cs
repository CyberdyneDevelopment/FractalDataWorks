using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Graphs.Describing;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the check that a column a UuidV7 value source fills can hold a Guid (FDW-3400): the type is the data set field's, so the check
/// reads the fields and refuses a column that is not a Guid field, naming it.
/// </summary>
public sealed class DestinationGuidColumnsTests
{
    private static GraphPipelineConfiguration Graph(string kind = "UuidV7", string column = "PublicId", string dataSet = "Orders")
    {
        var destination = new PipelineComponentConfiguration
        {
            Name = "load", ComponentType = "DataSetDestination", DataSetName = dataSet, LoadKind = "Insert",
        };
        destination.ValueSources.Add(new PipelineComponentValueSourceConfiguration { Ordinal = 0, ColumnName = column, SourceKind = kind });
        var graph = new GraphPipelineConfiguration { Name = "p" };
        graph.Components.Add(destination);
        return graph;
    }

    private static Func<string, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphField>>>> Fields(params GraphField[] fields) =>
        (_, _) => Task.FromResult<IGenericResult<IReadOnlyList<GraphField>>>(GenericResult<IReadOnlyList<GraphField>>.Success(fields));

    private static Task<IReadOnlyList<GraphProblem>> Check(GraphPipelineConfiguration graph, Func<string, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphField>>>> fields) =>
        DestinationGuidColumns.Problems(graph, fields, NullLogger.Instance, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AGuidFieldIsAccepted()
    {
        var problems = await Check(Graph(), Fields(new GraphField("PublicId", "Guid")));

        problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheColumnMatchesItsFieldIgnoringCase()
    {
        var problems = await Check(Graph(column: "publicid"), Fields(new GraphField("PublicId", "Guid")));

        problems.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("String")]
    [InlineData("Int64")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFieldOfAnotherTypeIsRefusedNamingTheColumnAndTheType(string type)
    {
        var problems = await Check(Graph(), Fields(new GraphField("PublicId", type)));

        var problem = problems.ShouldHaveSingleItem();
        problem.Path.ShouldBe(new[] { "components", "load", "settings", "valueSources" });
        problem.Reason.ShouldContain("'PublicId'");
        problem.Reason.ShouldContain("UuidV7");
        problem.Reason.ShouldContain($"is of type {type}, not Guid");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AColumnThatIsNotAFieldOfTheDataSetIsRefusedNamingIt()
    {
        var problems = await Check(Graph(), Fields(new GraphField("Id", "Guid")));

        var problem = problems.ShouldHaveSingleItem();
        problem.Reason.ShouldContain("'PublicId'");
        problem.Reason.ShouldContain("is not a field of data set 'Orders'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFieldWithNoTypeIsRefused()
    {
        var problems = await Check(Graph(), Fields(new GraphField("PublicId", null)));

        problems.ShouldHaveSingleItem().Reason.ShouldContain("has no type");
    }

    [Theory]
    [InlineData("Constant")]
    [InlineData("CurrentTimestamp")]
    [InlineData("NextSequenceValue")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKindThatMintsNoGuidIsNotJudgedAndItsDataSetIsNotRead(string kind)
    {
        var read = false;
        var problems = await Check(Graph(kind), (_, _) =>
        {
            read = true;
            return Fields(new GraphField("PublicId", "String"))("Orders", CancellationToken.None);
        });

        problems.ShouldBeEmpty();
        read.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADataSetThatCannotBeReadIsTheRunsToRefuse()
    {
        var problems = await Check(
            Graph(),
            (_, _) => Task.FromResult<IGenericResult<IReadOnlyList<GraphField>>>(GenericResult<IReadOnlyList<GraphField>>.Failure(PipelineLoadLog.ValueSourceColumnsNotChecked(NullLogger.Instance, "test", "Orders"))));

        problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADataSetNamedByVariableIsNotKnownWhenTheGraphIsSaved()
    {
        var read = false;
        var problems = await Check(Graph(dataSet: "{{Target}}"), (_, _) =>
        {
            read = true;
            return Fields(new GraphField("PublicId", "String"))("x", CancellationToken.None);
        });

        problems.ShouldBeEmpty();
        read.ShouldBeFalse();
    }
}
