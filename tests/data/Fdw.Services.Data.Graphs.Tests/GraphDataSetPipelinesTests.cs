using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Graphs;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>FDW-5291: a Graph data set's stored graph is read as the graph the engine runs, member for member.</summary>
public sealed class GraphDataSetPipelinesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public void ComponentsLinksAndTheirChildrenAreCopiedAndTheLinkCapacityIsTheOneStated()
    {
        var dataSet = new GraphDataSetConfiguration { Id = Guid.NewGuid(), Name = "Weather", Implementation = "Graph" };
        var source = new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource", DataSetName = "Readings", BatchSize = 50 };
        var map = new GraphDataSetComponentConfiguration { Name = "map", ComponentType = "Map" };
        map.Fields.Add(new GraphDataSetComponentFieldConfiguration { SettingName = "mappings", InputName = "input", TargetField = "Day", SourceField = "ReadAt" });
        dataSet.Components.Add(source);
        dataSet.Components.Add(map);
        dataSet.Links.Add(new GraphDataSetLinkConfiguration { FromComponent = "src", FromOutput = "output", ToComponent = "map", ToInput = "input" });

        var graph = GraphDataSetPipelines.Read(dataSet, 7);

        graph.Name.ShouldBe("Weather");
        graph.Id.ShouldBe(dataSet.Id);
        graph.LinkCapacity.ShouldBe(7);
        graph.Components.Select(c => c.Name).ShouldBe(["src", "map"]);
        graph.Components[0].DataSetName.ShouldBe("Readings");
        graph.Components[0].BatchSize.ShouldBe(50);
        graph.Components[0].ParentComponent.ShouldBeNull();
        graph.Components[1].Fields.Single().TargetField.ShouldBe("Day");
        graph.Links.Single().ToComponent.ShouldBe("map");
        graph.DroppedOutputs.ShouldBeEmpty();
    }
}
