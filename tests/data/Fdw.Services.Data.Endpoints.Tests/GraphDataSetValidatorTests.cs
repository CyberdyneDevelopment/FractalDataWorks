using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Data.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-5290: the draft of POST /datasets/graph is checked without being saved: every problem saving would refuse, each with its code and path, and intended sources apart.</summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5290")]
public sealed class GraphDataSetValidatorTests
{
    private static GraphFieldOutRequest Field(string name) => new() { Name = name, IsFilterable = true, IsNullable = true };

    private static string CodeOf(string name) => DataServiceResultCodes.ByName(name).Code;

    private static async Task<GraphDataSetValidationPayload> Validate(CreateGraphDataSetRequest request, StubGraphDataSetSources sources) =>
        (await GraphDataSetValidator.Validate(request, sources, NullLogger.Instance, TestContext.Current.CancellationToken)).Value!;

    [Fact]
    public async Task AnAbstractDraftWithIntendedSourcesIsValidAndReportsThemNotBound()
    {
        var request = new CreateGraphDataSetRequest { Name = "CustomerSegments", FieldsOut = [Field("Segment")] };
        request.IntendedSources.Add(new GraphIntendedSourceRequest { Kind = "DataStore", DataStore = "Weather feed" });
        request.IntendedSources.Add(new GraphIntendedSourceRequest { Kind = "DataSet", DataSet = "RawCustomers" });

        var answer = await Validate(request, new StubGraphDataSetSources().WithDataSet("RawCustomers", isUnboundGraph: true));

        answer.Problems.ShouldBeEmpty();
        answer.IsValid.ShouldBeTrue();
        answer.NotBound.Select(source => (string.Join('/', source.Path), source.Name, source.Exists)).ShouldBe(
            new[] { ("intendedSources/0", "Weather feed", false), ("intendedSources/1", "RawCustomers", true) });
    }

    [Fact]
    public async Task ADestinationAndAFieldNothingFeedsAreBothListedWithTheirPaths()
    {
        var request = new CreateGraphDataSetRequest
        {
            Name = "Revenue",
            FieldsOut = [Field("Amount")],
            Components = [new GraphComponentPayload { Name = "Out", Kind = "ContainerDestination" }],
        };

        var answer = await Validate(request, new StubGraphDataSetSources());

        answer.IsValid.ShouldBeFalse();
        var found = answer.Problems.Select(problem => (problem.Code, string.Join('/', problem.Path))).ToList();
        found.ShouldContain((CodeOf("GraphDataSetDestinationRefused"), "components/Out"));
        found.ShouldContain((CodeOf("GraphDataSetFieldOutUnfed"), "fieldsOut/Amount"));
    }

    [Fact]
    public async Task ASourceThatDoesNotExistIsListedAtItsComponent()
    {
        var request = new CreateGraphDataSetRequest
        {
            Name = "Revenue",
            FieldsOut = [Field("Amount")],
            Components =
            [
                new GraphComponentPayload
                {
                    Name = "Teams",
                    Kind = "DataSetSource",
                    Settings = new Dictionary<string, object?>(System.StringComparer.OrdinalIgnoreCase) { ["dataSet"] = "NoSuchSet", ["batchSize"] = 1000 },
                },
            ],
        };

        var answer = await Validate(request, new StubGraphDataSetSources());

        answer.Problems.Select(problem => (problem.Code, string.Join('/', problem.Path))).ShouldContain((CodeOf("GraphDataSetSourceMissing"), "components/Teams"));
    }

    private static GraphFieldOutRequest Fed(string name, string component) =>
        new() { Name = name, DataType = "String", IsFilterable = true, IsNullable = true, FedByComponent = component, FedByField = name };

    private static GraphComponentPayload Source(string name) => new()
    {
        Name = name,
        Kind = "DataSetSource",
        Settings = new Dictionary<string, object?>(System.StringComparer.OrdinalIgnoreCase) { ["dataSet"] = "NflTeams", ["batchSize"] = 3 },
    };

    private static CreateGraphDataSetRequest OneSource(string fieldsOutComponent, string fieldsOutOutput, params GraphComponentPayload[] components) => new()
    {
        Name = "Teams",
        FieldsOut = [Fed("Name", fieldsOutComponent)],
        FieldsOutComponent = fieldsOutComponent,
        FieldsOutOutput = fieldsOutOutput,
        Components = [.. components],
    };

    [Fact]
    public async Task ADraftOfOneSourceWhoseOutputIsTheStatedFieldsOutIsValid()
    {
        var answer = await Validate(OneSource("src", "output", Source("src")), new StubGraphDataSetSources().WithDataSet("NflTeams"));

        answer.Problems.ShouldBeEmpty();
        answer.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task AnOutputThatIsNeitherLinkedDroppedNorTheStatedFieldsOutIsStillReportedAsLost()
    {
        var answer = await Validate(OneSource("src", "output", Source("src"), Source("other")), new StubGraphDataSetSources().WithDataSet("NflTeams"));

        answer.Problems.Select(problem => (problem.Code, string.Join('/', problem.Path))).ShouldContain((CodeOf("GraphDataSetComponentInvalid"), "components/other/outputs/output"));
    }

    [Fact]
    public async Task AFieldsOutThatNamesAnOutputTheComponentDoesNotHaveAnswersAProblemAtFieldsOutOutputAndDoesNotThrow()
    {
        var answer = await Validate(OneSource("src", "outputt", Source("src")), new StubGraphDataSetSources().WithDataSet("NflTeams"));

        answer.IsValid.ShouldBeFalse();
        var paths = answer.Problems.Select(problem => string.Join('/', problem.Path)).ToList();
        paths.ShouldContain("fieldsOutOutput");
        paths.ShouldNotContain(path => path.StartsWith("links", System.StringComparison.Ordinal));
    }

    [Fact]
    public async Task AFieldsOutThatNamesAComponentTheGraphDoesNotHaveAnswersAProblemAtFieldsOutComponent()
    {
        var answer = await Validate(OneSource("nothere", "output", Source("src")), new StubGraphDataSetSources().WithDataSet("NflTeams"));

        answer.IsValid.ShouldBeFalse();
        answer.Problems.Select(problem => string.Join('/', problem.Path)).ShouldContain("fieldsOutComponent");
    }

    [Fact]
    public async Task TheDraftIsAnsweredWithTheSameCheckForARouteThatReadsIt()
    {
        var request = OneSource("src", "output", Source("src"));

        var answer = (await GraphDataSetValidator.Draft(request, new StubGraphDataSetSources().WithDataSet("NflTeams"), NullLogger.Instance, TestContext.Current.CancellationToken)).Value!;

        answer.Payload.IsValid.ShouldBeTrue();
        answer.Draft.Components.Select(component => component.Name).ShouldBe(["src"]);
        answer.Draft.FieldsOutComponent.ShouldBe("src");
        answer.Draft.FieldsOutOutput.ShouldBe("output");
        answer.Draft.GraphFields.Single().FedByComponent.ShouldBe("src");
    }
}
