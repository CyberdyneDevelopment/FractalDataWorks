using System.Text.Json;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="PipelineLoadDraft"/> (FDW-1110, contract item 6): a load definition is judged against the data
/// sets it touches, and every problem lands on the stage -- the load step's name -- it belongs to.
/// </summary>
public sealed class PipelineLoadDraftTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyList<(string? Stage, string? Expression)> NoExpressions = [];

    private static IDataSetImplementationConfiguration DataSetOf(string name, string connection, params string[] fields)
    {
        var dataSet = new Mock<IDataSetImplementationConfiguration>();
        dataSet.SetupGet(d => d.Name).Returns(name);
        dataSet.SetupGet(d => d.Fields).Returns([.. fields.Select(f => new DataSetFieldConfiguration { Name = f })]);
        dataSet.SetupGet(d => d.Sources).Returns([new DataSetSourceConfiguration { ConnectionName = connection, DataStoreName = "Store", ContainerName = name }]);
        return dataSet.Object;
    }

    private static PipelineLoadDraft Draft(params IDataSetImplementationConfiguration[] dataSets)
    {
        var provider = new Mock<IDataSetConfigurationProvider>();
        foreach (var dataSet in dataSets)
        {
            provider
                .Setup(p => p.Get(dataSet.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(dataSet));
        }

        var stores = new DataStoreConfigurationProvider(
            NullLogger<DataStoreConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "Store", "Path", "Table");
        return new PipelineLoadDraft(provider.Object, stores, new Mock<IConnectionConfigurationProvider>().Object, NullLogger.Instance);
    }

    private static CreatePipelineRequest Body(string json) => JsonSerializer.Deserialize<CreatePipelineRequest>(json, Web)!;

    private const string Upsert = """
        { "loadMode": "Upsert", "loadOptions": { "keyColumns": ["Id"], "compareColumns": ["Name"], "writeColumns": ["Id", "Name"] } }
        """;

    private const string UpsertWithoutCompare = """
        { "loadMode": "Upsert", "loadOptions": { "keyColumns": ["Id"], "compareColumns": [], "writeColumns": ["Id", "Name"] } }
        """;

    private const string TwoSteps = """
        { "loadSteps": [
            { "order": 1, "name": "Upsert orders", "isEnabled": true, "kind": "BulkUpsert", "destinationDataSet": "Orders",
              "options": { "keyColumns": ["Id"], "compareColumns": [], "writeColumns": ["Id", "Nmae"] } },
            { "order": 2, "name": "Copy", "isEnabled": true, "kind": "InsertFromQuery", "destinationDataSet": "History",
              "options": { "sourceDataSet": "Remote", "writeColumns": ["Name"], "filterExpression": "UpdatedAt > {{Since}}" } } ] }
        """;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnUpsertWhoseColumnsAreOnTheDestinationHasNoProblems()
    {
        var draft = Draft(DataSetOf("Orders", "Main", "Id", "Name"));

        var problems = await draft.Problems(Body(Upsert), "Orders", NoExpressions, TestContext.Current.CancellationToken);

        problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AColumnThatIsNotOnTheDestinationIsAProblemOfThePipelinesOwnLoad()
    {
        var draft = Draft(DataSetOf("Orders", "Main", "Id"));

        var problems = await draft.Problems(Body(UpsertWithoutCompare), "Orders", NoExpressions, TestContext.Current.CancellationToken);

        var problem = problems.ShouldHaveSingleItem();
        problem.Stage.ShouldBeNull();
        problem.Field.ShouldBe("LoadOptions.WriteColumns");
        problem.Reason.ShouldContain("'Name'");
        problem.Reason.ShouldContain("'Orders'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ColumnsAreRefusedWhenThePipelineNamesNoDestinationDataSetToCheckThemAgainst()
    {
        var draft = Draft();

        var problems = await draft.Problems(Body(Upsert), null, NoExpressions, TestContext.Current.CancellationToken);

        problems.ShouldHaveSingleItem().Reason.ShouldContain("none is named");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task EachProblemOfALoadStepIsPlacedOnThatStepsName()
    {
        var draft = Draft(
            DataSetOf("Orders", "Main", "Id", "Name"),
            DataSetOf("History", "Main", "Name"),
            DataSetOf("Remote", "Elsewhere", "Name", "UpdatedAt"));

        var problems = await draft.Problems(Body(TwoSteps), "Orders", NoExpressions, TestContext.Current.CancellationToken);

        problems.Select(p => (p.Stage, p.Field)).ShouldBe(
        [
            ("Upsert orders", "Options.WriteColumns"),
            ("Copy", "Options.SourceDataSet"),
            ("Copy", "FilterExpression"),
        ]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARunParameterTheFilterReferencesMustBeDeclaredOrReserved()
    {
        var draft = Draft(DataSetOf("Orders", "Main", "Id", "Name"));
        var body = Body(Upsert);
        body.Parameters = [new PipelineParameterDeclaration { Name = "Since", Type = "DateTimeOffset" }];
        var expressions = new List<(string? Stage, string? Expression)>
        {
            ("Recent", "UpdatedAt > {{Since}} AND Id > {{LastSuccessfulRun}} AND Region = {{Region}}"),
        };

        var problems = await draft.Problems(body, "Orders", expressions, TestContext.Current.CancellationToken);

        var problem = problems.ShouldHaveSingleItem();
        problem.Stage.ShouldBe("Recent");
        problem.Reason.ShouldContain("'Region'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AStructuralProblemOfAStepIsPlacedOnThatStepNotOnThePipeline()
    {
        var draft = Draft(DataSetOf("Orders", "Main", "Id", "Name"));
        var body = Body("""
            { "loadSteps": [ { "order": 1, "name": "Update orders", "isEnabled": true, "kind": "Update", "destinationDataSet": "Orders",
                               "options": { "writeColumns": ["Name"] } } ] }
            """);

        var problems = await draft.Problems(body, "Orders", NoExpressions, TestContext.Current.CancellationToken);

        var problem = problems.ShouldHaveSingleItem();
        problem.Stage.ShouldBe("Update orders");
        problem.Reason.ShouldContain("key columns");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AProblemTheSchemaCheckFindsOnAStepCarriesThatStepsPathLikeAStructuralOne()
    {
        var draft = Draft(
            DataSetOf("Orders", "Main", "Id", "Name"),
            DataSetOf("History", "Main", "Name"),
            DataSetOf("Remote", "Elsewhere", "Name", "UpdatedAt"));

        var problems = await draft.Problems(Body(TwoSteps), "Orders", NoExpressions, TestContext.Current.CancellationToken);

        problems.Select(p => p.StagePath).ShouldAllBe(path => path != null);
        problems[0].StagePath.ShouldBe(new[] { "Upsert orders" });
        problems[1].StagePath.ShouldBe(new[] { "Copy" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusedStructureAnswersTheCallerWith400AndTheReason()
    {
        var draft = Draft(DataSetOf("Orders", "Main", "Id", "Name"));
        var body = Body("""
            { "loadSteps": [ { "order": 1, "name": "Update orders", "isEnabled": true, "kind": "Update", "destinationDataSet": "Orders",
                               "options": { "writeColumns": ["Name"] } } ] }
            """);

        var prepared = await draft.Prepare(body, "Orders", NoExpressions, TestContext.Current.CancellationToken);

        prepared.IsFailure.ShouldBeTrue();
        var (status, problem) = ResultHttpStatusMapper.Map(prepared, new DefaultHttpContext());
        status.ShouldBe(400);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("key columns");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ARefusedSchemaCheckAnswersTheCallerWith400AndTheReason()
    {
        var draft = Draft(DataSetOf("Orders", "Main", "Id"));

        var prepared = await draft.Prepare(Body(UpsertWithoutCompare), "Orders", NoExpressions, TestContext.Current.CancellationToken);

        prepared.IsFailure.ShouldBeTrue();
        var (status, problem) = ResultHttpStatusMapper.Map(prepared, new DefaultHttpContext());
        status.ShouldBe(400);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain("'Name'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PrepareAnswersTheMappedLoadWhenTheDefinitionIsValid()
    {
        var draft = Draft(DataSetOf("Orders", "Main", "Id", "Name"));

        var prepared = await draft.Prepare(Body(Upsert), "Orders", NoExpressions, TestContext.Current.CancellationToken);

        prepared.IsSuccess.ShouldBeTrue(prepared.CurrentMessage);
        prepared.Value!.LoadMode.ShouldBe("Upsert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PrepareRefusesWithTheFirstProblemNamingItsStageAndField()
    {
        var draft = Draft(DataSetOf("Orders", "Main", "Id"));

        var prepared = await draft.Prepare(Body(UpsertWithoutCompare), "Orders", NoExpressions, TestContext.Current.CancellationToken);

        prepared.IsFailure.ShouldBeTrue();
        prepared.CurrentMessage.ShouldNotBeNull().ShouldContain("LoadOptions.WriteColumns");
        prepared.CurrentMessage.ShouldNotBeNull().ShouldContain("'Name'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ADestinationWithTwoSourcesIsRefusedAsTheEngineWouldRefuseItAtRun()
    {
        var joined = new Mock<IDataSetImplementationConfiguration>();
        joined.SetupGet(d => d.Name).Returns("Orders");
        joined.SetupGet(d => d.Fields).Returns([new DataSetFieldConfiguration { Name = "Id" }, new DataSetFieldConfiguration { Name = "Name" }]);
        joined.SetupGet(d => d.Sources).Returns(
        [
            new DataSetSourceConfiguration { ConnectionName = "Main", DataStoreName = "Store", ContainerName = "A" },
            new DataSetSourceConfiguration { ConnectionName = "Main", DataStoreName = "Store", ContainerName = "B" },
        ]);
        var draft = Draft(joined.Object);

        var problems = await draft.Problems(Body(Upsert), "Orders", NoExpressions, TestContext.Current.CancellationToken);

        var problem = problems.ShouldHaveSingleItem();
        problem.Field.ShouldBe("DestinationDataSet");
        problem.Reason.ShouldContain("exactly one source");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ADestinationWhoseSourceNamesNoContainerIsRefused()
    {
        var bare = new Mock<IDataSetImplementationConfiguration>();
        bare.SetupGet(d => d.Name).Returns("Orders");
        bare.SetupGet(d => d.Fields).Returns([new DataSetFieldConfiguration { Name = "Id" }, new DataSetFieldConfiguration { Name = "Name" }]);
        bare.SetupGet(d => d.Sources).Returns([new DataSetSourceConfiguration { ConnectionName = "Main", DataStoreName = "Store" }]);
        var draft = Draft(bare.Object);

        var problems = await draft.Problems(Body(Upsert), "Orders", NoExpressions, TestContext.Current.CancellationToken);

        problems.ShouldHaveSingleItem().Reason.ShouldContain("names no container");
    }
}
