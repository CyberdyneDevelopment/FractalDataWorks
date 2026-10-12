using System.Text.Json;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="SavedPipelineCheck"/> (FDW-1105): an edit of a saved pipeline is judged as the update would judge
/// it, every problem is listed, a load problem lands on the stage it belongs to, and nothing is written.
/// </summary>
public sealed class SavedPipelineCheckTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static IDataSetImplementationConfiguration DataSetOf(string name, params string[] fields)
    {
        var dataSet = new Mock<IDataSetImplementationConfiguration>();
        dataSet.SetupGet(d => d.Name).Returns(name);
        dataSet.SetupGet(d => d.Fields).Returns([.. fields.Select(f => new DataSetFieldConfiguration { Name = f })]);
        dataSet.SetupGet(d => d.Sources).Returns([new DataSetSourceConfiguration { ConnectionName = "Main", DataStoreName = "Store", ContainerName = name }]);
        return dataSet.Object;
    }

    private static SavedPipelineCheck Check()
    {
        var dataSets = new Mock<IDataSetConfigurationProvider>();
        foreach (var dataSet in new[] { DataSetOf("Orders", "Id", "Name"), DataSetOf("OrdersCopy", "Id", "Name"), DataSetOf("Rejects", "Id", "Name", "RejectReason", "RejectStage") })
        {
            dataSets
                .Setup(p => p.Get(dataSet.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(dataSet));
        }

        var connections = new Mock<IConnectionConfigurationProvider>();
        var stores = new DataStoreConfigurationProvider(
            NullLogger<DataStoreConfigurationProvider>.Instance, new Mock<IConfigurationGatewayProvider>().Object, "Store", "Path", "Table");
        return new SavedPipelineCheck(
            new PipelineLoadDraft(dataSets.Object, stores, connections.Object, NullLogger.Instance),
            new StageChainLookup(new Mock<ITransformationConfigurationProvider>().Object, NullLogger.Instance),
            NullLogger.Instance);
    }

    private static string RejectStep(string destination, params string[] writeColumns) =>
        "{ \"order\": 1, \"name\": \"Keep rejects\", \"isEnabled\": true, \"kind\": \"BulkInsert\", \"destinationDataSet\": \"" + destination
        + "\", \"options\": { \"writeColumns\": [" + string.Join(", ", writeColumns.Select(c => "\"" + c + "\"")) + "] }, \"valueSources\": [] }";

    private static BatchCopyPipelineConfiguration Saved(string? loadMode) =>
        new() { Name = "Nightly", Implementation = "BatchCopy", SourceDataSet = "Orders", DestinationDataSet = "OrdersCopy", LoadMode = loadMode };

    private static UpdatePipelineRequest Body(string json) => JsonSerializer.Deserialize<UpdatePipelineRequest>(json, Web)!;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnEditThatIsFineReportsNothingAndNeverReportsThePipelinesOwnNameAsTaken()
    {
        var problems = await Check().Problems(
            Body("""{ "name": "Nightly", "description": "nightly copy" }"""),
            Saved("Append"), null, TestContext.Current.CancellationToken);

        problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnEditOfALoadStepThatBreaksItIsReportedOnThatStep()
    {
        const string edit = """
            { "name": "Nightly",
              "loadSteps": [
                { "order": 1, "name": "Upsert orders", "isEnabled": true, "kind": "BulkUpsert", "destinationDataSet": "OrdersCopy",
                  "options": { "keyColumns": ["Id"], "compareColumns": [], "writeColumns": ["Id", "Nmae"] } } ] }
            """;

        var problems = await Check().Problems(Body(edit), Saved("Append"), null, TestContext.Current.CancellationToken);

        var problem = problems.ShouldHaveSingleItem();
        problem.Stage.ShouldBe("Upsert orders");
        problem.Field.ShouldBe("Options.WriteColumns");
        problem.Reason.ShouldContain("'Nmae'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task StatingOnlyTheOptionsReplacesTheWholeUnitSoTheStoredLoadModeIsGone()
    {
        const string edit = """
            { "name": "Nightly",
              "loadOptions": { "keyColumns": ["Id"], "compareColumns": ["Name"], "writeColumns": ["Id", "Name"] } }
            """;

        var problems = await Check().Problems(Body(edit), Saved("Upsert"), null, TestContext.Current.CancellationToken);

        var problem = problems.ShouldHaveSingleItem();
        problem.Stage.ShouldBeNull();
        problem.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ATruncateStatedTrueBesideALoadModeThatDoesNotTruncateIsAProblemOfThePipeline()
    {
        var problems = await Check().Problems(
            Body("""{ "name": "Nightly", "truncateBeforeLoad": true }"""),
            Saved("Append"), null, TestContext.Current.CancellationToken);

        var problem = problems.ShouldHaveSingleItem();
        problem.Field.ShouldBe("TruncateBeforeLoad");
        problem.Stage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ATruncateStatedFalseIsAccepted()
    {
        var problems = await Check().Problems(
            Body("""{ "name": "Nightly", "truncateBeforeLoad": false }"""),
            Saved("Append"), null, TestContext.Current.CancellationToken);

        problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task EveryProblemIsListedNotJustTheFirst()
    {
        const string edit = """
            { "name": "Nightly", "truncateBeforeLoad": true,
              "loadSteps": [
                { "order": 1, "name": "Upsert orders", "isEnabled": true, "kind": "BulkUpsert", "destinationDataSet": "OrdersCopy",
                  "options": { "keyColumns": ["Id"], "compareColumns": [], "writeColumns": ["Id", "Nmae"] } } ] }
            """;

        var problems = await Check().Problems(Body(edit), Saved("Append"), null, TestContext.Current.CancellationToken);

        problems.Select(p => p.Field).ShouldContain("TruncateBeforeLoad");
        problems.Select(p => p.Stage).ShouldContain("Upsert orders");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AUnionStatedForASavedStreamingPipelineIsAProblemOfThatStage()
    {
        const string edit = """
            { "name": "Nightly",
              "transforms": [
                { "name": "AddOrders", "operationType": "Union", "executionOrder": 1, "unionSource": "Orders",
                  "fieldMappings": [ { "name": "Id", "sourceField": "Id", "destinationField": "Id" } ] } ] }
            """;
        var streaming = new BatchCopyPipelineConfiguration
        {
            Name = "Nightly", Implementation = "Streaming", SourceDataSet = "Orders", DestinationDataSet = "OrdersCopy", LoadMode = "Append"
        };

        var problems = await Check().Problems(Body(edit), streaming, null, TestContext.Current.CancellationToken);

        var problem = problems.ShouldHaveSingleItem();
        problem.Stage.ShouldBe("AddOrders");
        problem.Reason.ShouldContain("Streaming");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task RejectStepsStatedAloneAreJudgedAgainstTheColumnsOfTheDataSetTheyName()
    {

        var problems = await Check().Problems(
            Body("{ \"name\": \"Nightly\", \"rejectSteps\": [" + RejectStep("Orders", "RejectReason", "RejectStage") + "] }"),
            Saved("Append"), null, TestContext.Current.CancellationToken);

        problems.Any(p => p.Stage == "Keep rejects" && p.Reason.Contains("'RejectReason' is not a field of data set 'Orders'", StringComparison.Ordinal)).ShouldBeTrue();
        problems.Any(p => p.Stage == "Keep rejects" && p.Reason.Contains("'RejectStage' is not a field of data set 'Orders'", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task RejectStepsToADataSetWithTheReservedColumnsAreAccepted()
    {

        var problems = await Check().Problems(
            Body("{ \"name\": \"Nightly\", \"rejectSteps\": [" + RejectStep("Rejects", "Id", "Name", "RejectReason", "RejectStage") + "] }"),
            Saved("Append"), null, TestContext.Current.CancellationToken);

        problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task RejectStepsToTheDestinationOfTheStoredLoadAreAProblemOfThatStep()
    {

        var problems = await Check().Problems(
            Body("{ \"name\": \"Nightly\", \"rejectSteps\": [" + RejectStep("OrdersCopy", "Id", "Name", "RejectReason", "RejectStage") + "] }"),
            Saved("Append"), null, TestContext.Current.CancellationToken);

        problems.Any(p => p.Stage == "Keep rejects" && p.Reason.Contains("writes to", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AnEmptyRejectStepListRemovesThemAndIsAcceptedAsAChange()
    {

        var problems = await Check().Problems(
            Body("""{ "name": "Nightly", "rejectSteps": [] }"""),
            Saved("Append"), null, TestContext.Current.CancellationToken);

        problems.ShouldBeEmpty();
    }
}
