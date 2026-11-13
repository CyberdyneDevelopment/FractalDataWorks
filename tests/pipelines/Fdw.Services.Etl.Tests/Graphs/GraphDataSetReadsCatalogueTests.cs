using System.Linq;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-5296: lineage and status read what a Graph data set reads from the settings its components hold, with no list of component kinds. The component catalogue states which
/// kinds take a data set, data store or container setting; these tests walk the whole catalogue against that reading, so a kind added to it that names a source fails here
/// until the reading covers it.
/// </summary>
public sealed class GraphDataSetReadsCatalogueTests
{
    private const string DataSetName = "SomeDataSet";
    private const string StoreName = "SomeStore";
    private const string ContainerName = "SomeContainer";

    private static bool Declares(IComponentType kind, string settingKind) => kind.Settings.Any(setting => setting.Kind == settingKind);

    private static GraphDataSetConfiguration GraphOf(IComponentType kind) => new()
    {
        Name = "Revenue",
        Components =
        [
            new GraphDataSetComponentConfiguration
            {
                Name = "only", ComponentType = kind.Name, DataSetName = DataSetName, DataStoreName = StoreName, ContainerName = ContainerName,
            },
        ],
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ACatalogueKindThatDeclaresADataSetSettingIsReadAsReadingADataSet()
    {
        foreach (var kind in ComponentTypes.All().Where(kind => Declares(kind, "DataSet") && kind.Outputs.Count > 0))
        {
            GraphDataSetReads.Of(GraphOf(kind)).Any(read => read.Kind == GraphDataSetReadKinds.DataSet && read.Name == DataSetName).ShouldBeTrue(kind.Name);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ACatalogueKindThatDeclaresAContainerSettingIsReadAsReadingAContainerOfItsStore()
    {
        foreach (var kind in ComponentTypes.All().Where(kind => Declares(kind, "Container") && kind.Outputs.Count > 0))
        {
            GraphDataSetReads.Of(GraphOf(kind)).Any(read => read.Kind == GraphDataSetReadKinds.Container && read.Name == ContainerName && read.Parents.SequenceEqual([StoreName])).ShouldBeTrue(kind.Name);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ACatalogueKindThatDeclaresOnlyADataStoreSettingIsReadAsSelectingFromTheStore()
    {
        var storeOnly = ComponentTypes.All().Where(kind => Declares(kind, "DataStore") && !Declares(kind, "Container") && kind.Outputs.Count > 0).ToList();

        storeOnly.ShouldNotBeEmpty();
        foreach (var kind in storeOnly)
        {
            var graph = GraphOf(kind);
            graph.Components[0].DataSetName = null;
            graph.Components[0].ContainerName = null;
            GraphDataSetReads.Of(graph).Any(read => read.Kind == GraphDataSetReadKinds.DataStore && read.Name == StoreName).ShouldBeTrue(kind.Name);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheKindsThatNameASourceTodayAreTheOnesTheReadingWasBuiltFor()
    {
        var naming = ComponentTypes.All()
            .Where(kind => kind.Outputs.Count > 0 && (Declares(kind, "DataSet") || Declares(kind, "Container") || Declares(kind, "DataStore")))
            .Select(kind => kind.Name)
            .Order()
            .ToArray();

        naming.ShouldBe(["ContainerSource", "DataSetSource", "SqlSource"]);
    }
}
