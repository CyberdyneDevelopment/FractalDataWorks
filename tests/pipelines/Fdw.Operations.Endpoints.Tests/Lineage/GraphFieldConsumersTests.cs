using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Endpoints;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Describing;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// FDW-2157: the field lineage of a data set names the graph pipelines that use each field and what it becomes there, from the graph's own field flow.
/// A pipeline whose fields cannot be followed to a destination is named as not followed, never guessed at.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Etl")]
public class GraphFieldConsumersTests
{
    private static Task<IGenericResult<IReadOnlyList<GraphField>>> DataSetFields(string name, CancellationToken ct) =>
        Task.FromResult<IGenericResult<IReadOnlyList<GraphField>>>(name == "AfcPlayers"
            ? GenericResult<IReadOnlyList<GraphField>>.Success([new GraphField("FirstName", "String"), new GraphField("LastName", "String")])
            : GenericResult<IReadOnlyList<GraphField>>.Failure(new Fdw.Messages.GenericMessage($"no data set '{name}'")));

    private static GraphPipelineConfiguration Pipeline(string name, string reads, params (string Target, string? Source)[] maps)
    {
        var load = new PipelineComponentConfiguration { Name = "load", ComponentType = "DataSetDestination", DataSetName = "Out" };
        for (var i = 0; i < maps.Length; i++)
        {
            load.Fields.Add(new PipelineComponentFieldConfiguration
            {
                SettingName = "mappings", InputName = "input", Ordinal = i, TargetField = maps[i].Target, SourceField = maps[i].Source,
                ConstantType = maps[i].Source is null ? "String" : null, ConstantText = maps[i].Source is null ? "x" : null,
            });
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Domain = "Pipeline",
            Implementation = "Graph",
            IsEnabled = true,
            Components = [new PipelineComponentConfiguration { Name = "src", ComponentType = "DataSetSource", DataSetName = reads }, load],
            Links = [new PipelineLinkConfiguration { FromComponent = "src", FromOutput = "output", ToComponent = "load", ToInput = "input" }],
        };
    }

    private static Dictionary<string, List<FieldConsumerResponse>> Consumers() =>
        new(StringComparer.OrdinalIgnoreCase) { ["FirstName"] = [], ["LastName"] = [] };

    private static async Task<(Dictionary<string, List<FieldConsumerResponse>> Consumers, IReadOnlyList<string> NotFollowed)> Run(params GraphPipelineConfiguration[] graphs)
    {
        var consumers = Consumers();
        var notFollowed = await GraphFieldConsumers.Add(
            "AfcPlayers", new HashSet<string>(consumers.Keys, StringComparer.OrdinalIgnoreCase), consumers, graphs, DataSetFields,
            NullLogger.Instance, TestContext.Current.CancellationToken);
        return (consumers, notFollowed);
    }

    [Fact]
    public async Task APipelineIsNamedForAFieldItRenamesWithTheComponentAndTheNameItArrivesAs()
    {
        var (consumers, notFollowed) = await Run(Pipeline("NflPlayersLoad", "AfcPlayers", ("Name", "LastName")));

        var consumer = consumers["LastName"].ShouldHaveSingleItem();
        consumer.ConsumerKind.ShouldBe("Pipeline");
        consumer.ConsumerName.ShouldBe("NflPlayersLoad");
        consumer.ConsumerComponent.ShouldBe("load");
        consumer.ConsumerField.ShouldBe("Name");
        consumers["FirstName"].ShouldBeEmpty();
        notFollowed.ShouldBeEmpty();
    }

    [Fact]
    public async Task AFieldTheDestinationMakesItselfIsNotAConsumerAndThePipelineIsNamedAsNotFollowed()
    {
        var (consumers, notFollowed) = await Run(Pipeline("Constants", "AfcPlayers", ("Tag", null)));

        consumers.Values.ShouldAllBe(list => list.Count == 0);
        notFollowed.ShouldBe(["Constants"]);
    }

    [Fact]
    public async Task APipelineThatReadsAnotherDataSetIsNeitherAConsumerNorNotFollowed()
    {
        var (consumers, notFollowed) = await Run(Pipeline("Other", "People", ("Name", "Name")));

        consumers.Values.ShouldAllBe(list => list.Count == 0);
        notFollowed.ShouldBeEmpty();
    }

    [Fact]
    public async Task APipelineThatCannotBeDescribedIsNamedAsNotFollowed()
    {
        var broken = Pipeline("Broken", "AfcPlayers", ("Name", "LastName"));
        broken.Components.Add(new PipelineComponentConfiguration { Name = "extra", ComponentType = "DataSetSource", DataSetName = "Missing" });

        var (_, notFollowed) = await Run(broken);

        notFollowed.ShouldBe(["Broken"]);
    }
}
