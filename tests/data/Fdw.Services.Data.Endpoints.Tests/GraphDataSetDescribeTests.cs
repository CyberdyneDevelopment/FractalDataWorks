using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Describing;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-5504: a Graph data set draft is described by the pipeline describe's own mechanism, so a graph with no destination describes and a node is answered as the pipeline describe answers it.</summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5504")]
public sealed class GraphDataSetDescribeTests
{
    private static GraphPayload OneSource() => new()
    {
        Name = "Teams",
        Components =
        [
            new GraphComponentPayload
            {
                Name = "src",
                Kind = "DataSetSource",
                Settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["dataSet"] = "NflTeams", ["batchSize"] = 3 },
            },
        ],
    };

    private static DescribeContext Reading(params (string Name, string Type)[] fields) =>
        new((name, token) => Task.FromResult<IGenericResult<IReadOnlyList<GraphField>>>(
            GenericResult<IReadOnlyList<GraphField>>.Success([.. fields.Select(field => new GraphField(field.Name, field.Type))])));

    [Fact]
    public async Task AGraphWithNoDestinationDescribesTheFieldsAtTheSourcesOutput()
    {
        var graph = GraphPayloadReader.ReadDraft(OneSource(), Guid.Empty, []);
        var context = Reading(("Name", "String"), ("FoundedYear", "Int32"));

        var described = await GraphDescriber.Describe(graph.Value!, context, NullLogger.Instance, TestContext.Current.CancellationToken);

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        context.Problems.ShouldBeEmpty();
        var output = described.Value!.Select(GraphNodePayloads.Of).Single(node => node.Component == "src" && !node.IsInput && node.Node == "output");
        output.Fields.ShouldNotBeNull().Select(field => (field.Name, field.Type)).ShouldBe([("Name", "String"), ("FoundedYear", "Int32")]);
    }

    [Fact]
    public void ANodeTheEngineCouldNotDescribeIsAnsweredWithNoFieldsAndAFieldKeepsItsOrigin()
    {
        var unknown = GraphNodePayloads.Of(new GraphNodeFields("c", "output", false, null, null));
        var known = GraphNodePayloads.Of(new GraphNodeFields("c", "output", false, [new GraphField("Name", "String", new GraphFieldOrigin("NflTeams", "Name"))], null));

        unknown.Fields.ShouldBeNull();
        known.Fields.ShouldNotBeNull().Single().Origin.ShouldNotBeNull().DataSet.ShouldBe("NflTeams");
    }
}
