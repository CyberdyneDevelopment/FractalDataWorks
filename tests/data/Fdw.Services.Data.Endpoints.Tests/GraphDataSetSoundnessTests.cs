using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-5442: validate and save judge a Graph data set the same way, so a body validate calls sound is saved and one it calls unsound is refused with the problems listed.</summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5442")]
public sealed class GraphDataSetSoundnessTests
{
    private static GraphFieldOutRequest Fed(string name, string component) =>
        new() { Name = name, DataType = "String", IsFilterable = true, IsNullable = true, FedByComponent = component, FedByField = name };

    private static GraphFieldOutRequest Unfed(string name) => new() { Name = name, DataType = "String", IsFilterable = true, IsNullable = true };

    private static GraphComponentPayload Source(string name, string dataSet = "NflTeams") => new()
    {
        Name = name,
        Kind = "DataSetSource",
        Settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["dataSet"] = dataSet, ["batchSize"] = 3 },
    };

    private static CreateGraphDataSetRequest Draft(
        IList<GraphFieldOutRequest> fields, string? component, string? output, params GraphComponentPayload[] components) =>
        new() { Name = "Teams", FieldsOut = fields, FieldsOutComponent = component, FieldsOutOutput = output, Components = [.. components] };

    // Each body is made afresh: checking a draft reads the fields out of the request it is given.
    public static TheoryData<string, bool> Bodies() => new()
    {
        { "a bound draft whose output is the stated end", true },
        { "an abstract draft with an intended data store", true },
        { "a second output that is neither linked, dropped nor the end", false },
        { "an end that names a component the graph does not have", false },
        { "an end that names an output the component does not have", false },
        { "a field out nothing feeds", false },
        { "a source data set that does not exist", false },
        { "a destination component", false },
    };

    private static CreateGraphDataSetRequest Body(string name) => name switch
    {
        "a bound draft whose output is the stated end" => Draft([Fed("Name", "src")], "src", "output", Source("src")),
        "an abstract draft with an intended data store" => AbstractWithStore(),
        "a second output that is neither linked, dropped nor the end" => Draft([Fed("Name", "src")], "src", "output", Source("src"), Source("other")),
        "an end that names a component the graph does not have" => Draft([Fed("Name", "nothere")], "nothere", "output", Source("src")),
        "an end that names an output the component does not have" => Draft([Fed("Name", "src")], "src", "nope", Source("src")),
        "a field out nothing feeds" => Draft([Unfed("Name")], "src", "output", Source("src")),
        "a source data set that does not exist" => Draft([Fed("Name", "src")], "src", "output", Source("src", "NoSuchSet")),
        _ => Draft([Fed("Name", "Out")], "Out", "output", new GraphComponentPayload { Name = "Out", Kind = "ContainerDestination" }),
    };

    private static CreateGraphDataSetRequest AbstractWithStore()
    {
        var request = Draft([Unfed("Name")], null, null);
        request.IntendedSources.Add(new GraphIntendedSourceRequest { Kind = "DataStore", DataStore = "Weather feed" });
        return request;
    }

    private static StubGraphDataSetSources Sources() => new StubGraphDataSetSources().WithDataSet("NflTeams");

    // What saving does with a body: the steps it runs over the request, then the judgement of the data set they built (CreateGraphDataSetEndpointBase.Create).
    private static async Task<bool> SaveAccepts(CreateGraphDataSetRequest request)
    {
        var graph = new GraphDataSetConfiguration { Name = request.Name, Implementation = GraphDataSetConfiguration.ImplementationName };
        var values = new DataSetRequestValues(request.Name, NullLogger.Instance);
        if (GraphDataSetDefinitions.Apply(graph, request, values, GraphDataSetStamp.Checking, NullLogger.Instance).IsFailure)
        {
            return false;
        }

        var intents = await GraphDataSetDefinitions.Intents(graph, request.IntendedSources, new Mock<IDataSetConfigurationProvider>().Object, GraphDataSetStamp.Checking, NullLogger.Instance, TestContext.Current.CancellationToken);
        return intents.IsSuccess && (await GraphDataSetSoundness.Judge(graph, Sources(), NullLogger.Instance, TestContext.Current.CancellationToken)).IsSuccess;
    }

    [Theory]
    [MemberData(nameof(Bodies))]
    public async Task SaveAcceptsExactlyWhatValidateCallsSound(string body, bool sound)
    {
        var validated = (await GraphDataSetValidator.Validate(Body(body), Sources(), NullLogger.Instance, TestContext.Current.CancellationToken)).Value!;
        var saved = await SaveAccepts(Body(body));

        validated.IsValid.ShouldBe(sound, string.Join("; ", validated.Problems.Select(problem => $"{problem.Code} {string.Join('/', problem.Path)}")));
        saved.ShouldBe(validated.IsValid);
    }

    [Fact]
    public async Task AnUnsoundDataSetIsRefusedWithItsOwnCodeThatListsEachProblemWithItsPath()
    {
        var request = Body("a second output that is neither linked, dropped nor the end");
        var graph = new GraphDataSetConfiguration { Name = request.Name, Implementation = GraphDataSetConfiguration.ImplementationName };
        GraphDataSetDefinitions.Apply(graph, request, new DataSetRequestValues(request.Name, NullLogger.Instance), GraphDataSetStamp.Checking, NullLogger.Instance).IsSuccess.ShouldBeTrue();

        var refused = await GraphDataSetSoundness.Judge(graph, Sources(), NullLogger.Instance, TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
        refused.Code.ShouldBeOfType<GraphDataSetDraftNotSoundCode>();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain(DataServiceResultCodes.ByName("GraphDataSetComponentInvalid").Code);
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("components/other/outputs/output");
    }
}
