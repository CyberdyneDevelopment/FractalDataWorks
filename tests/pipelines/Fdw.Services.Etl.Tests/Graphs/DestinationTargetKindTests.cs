using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for what a destination writes (FDW-3193): the kinds Container, DataSet, File and Files, the settings each needs and refuses judged in one place
/// for the graph validator and the builder, with nothing assumed.
/// </summary>
public sealed class DestinationTargetKindTests
{
    private static PipelineComponentConfiguration Destination(
        string componentType, string? kind, string? physicalPath = null, string? physicalName = null, string? splitField = null, string? filesList = null) => new()
    {
        Name = "players",
        ComponentType = componentType,
        DataStoreName = componentType == "ContainerDestination" ? "Exports" : null,
        PathName = componentType == "ContainerDestination" ? "exports" : null,
        ContainerName = componentType == "ContainerDestination" ? "Players" : null,
        DataSetName = componentType == "DataSetDestination" ? "Players" : null,
        LoadKind = "Insert",
        TruncateFirst = false,
        TargetKind = kind,
        PhysicalPath = physicalPath,
        PhysicalName = physicalName,
        SplitField = splitField,
        FilesList = filesList,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheKindsSayWhatTheyNeedAndRefuseWhenTheyWriteAndWhetherTheyReadOrWrite()
    {
        TargetKinds.ByName("Container").WritesNeed.ShouldBe(["container"]);
        TargetKinds.ByName("DataSet").WritesNeed.ShouldBe(["dataSet"]);
        TargetKinds.ByName("File").WritesNeed.ShouldBe(["physicalName"]);
        TargetKinds.ByName("Files").WritesNeed.ShouldBe(["splitField"]);
        TargetKinds.ByName("Files").WritesRefuse.ShouldBe(["physicalName", "filesList"]);
        TargetKinds.ByName("DataSet").ForSources.ShouldBeFalse();
        TargetKinds.All().Where(kind => kind.ForSources).Select(kind => kind.Name).Order(StringComparer.Ordinal).ShouldBe(["Container", "File", "Files"]);
        TargetKinds.All().ShouldAllBe(kind => kind.ForDestinations);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheSplitFieldIsInTheCatalogueOfBothDestinationsAndTheBindingTableReadsAndWritesIt()
    {
        foreach (var type in new[] { "ContainerDestination", "DataSetDestination" })
        {
            var settings = ComponentTypes.ByName(type).Settings;
            settings.Single(setting => setting.Name == "splitField").Kind.ShouldBe("Text");
            settings.Single(setting => setting.Name == "targetKind").Kind.ShouldBe("TargetKinds");
        }

        var component = Destination("ContainerDestination", null);
        ComponentSettingBindings.Find("splitField")!.Set(component, "Team");
        component.SplitField.ShouldBe("Team");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("ContainerDestination", null, null, null, null, 0, "")]
    [InlineData("DataSetDestination", null, null, null, null, 0, "")]
    [InlineData("ContainerDestination", "Container", null, null, null, 0, "")]
    [InlineData("DataSetDestination", "DataSet", null, null, null, 0, "")]
    [InlineData("ContainerDestination", "File", "exports/x", "daily.csv", null, 0, "")]
    [InlineData("ContainerDestination", "Files", "exports/x", null, "Team", 0, "")]
    [InlineData("DataSetDestination", "Files", null, null, "Team", 0, "")]
    [InlineData("ContainerDestination", null, null, null, "Team", 1, "splitField")]
    [InlineData("ContainerDestination", null, null, "daily.csv", null, 1, "physicalName")]
    [InlineData("ContainerDestination", "Folder", null, null, null, 1, "targetKind")]
    [InlineData("ContainerDestination", "File", null, null, null, 1, "physicalName")]
    [InlineData("ContainerDestination", "Files", null, null, null, 1, "splitField")]
    [InlineData("ContainerDestination", "Files", null, "daily.csv", "Team", 1, "physicalName")]
    [InlineData("ContainerDestination", "File", null, "daily.csv", "Team", 1, "splitField")]
    [InlineData("ContainerDestination", "Container", null, "daily.csv", null, 1, "physicalName")]
    [InlineData("ContainerDestination", "DataSet", null, null, null, 1, "dataSet")]
    [InlineData("DataSetDestination", "Container", null, null, null, 1, "container")]
    public void ADestinationIsJudgedByTheKindItNames(
        string componentType, string? kind, string? physicalPath, string? physicalName, string? splitField, int problems, string field)
    {
        var found = TargetKindRules.WriteProblems(Destination(componentType, kind, physicalPath, physicalName, splitField));

        found.Count.ShouldBe(problems);
        if (problems > 0)
        {
            found.Single().Field.ShouldBe(field);
            found.Single().Message.ShouldContain("destination 'players'");
        }
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("DataSet", true)]
    [InlineData("Files", false)]
    public void ASourceIsJudgedByTheKindsForSourcesAndNoSourceTakesASplitField(string kind, bool kindIsForDestinationsOnly)
    {
        var source = new PipelineComponentConfiguration
        {
            Name = "files", ComponentType = "ContainerSource", DataStoreName = "Landing", ContainerName = "Orders", BatchSize = 50, TargetKind = kind, FilesList = "Names", SplitField = "Team",
        };

        var found = TargetKindRules.Problems(source);

        found.ShouldNotBeEmpty();
        (found.Any(problem => problem.Field == "targetKind")).ShouldBe(kindIsForDestinationsOnly);
        (found.Any(problem => problem.Field == "splitField")).ShouldBe(!kindIsForDestinationsOnly);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("File", "File")]
    [InlineData("Files", "Files")]
    [InlineData("Container", null)]
    [InlineData("DataSet", null)]
    [InlineData(null, null)]
    public void OnlyTheFileAndFilesKindsAddressADestinationAtAFile(string? kind, string? addressed)
    {
        var addressing = LoadAddressing.Of(kind, "exports", "daily.csv", "Team");

        addressing?.Kind.Name.ShouldBe(addressed);
        (addressing is null).ShouldBe(addressed is null);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("ContainerDestination", "Files", "splitField")]
    [InlineData("ContainerDestination", "File", "physicalName")]
    [InlineData("DataSetDestination", "Container", "container")]
    public void ADestinationThatStatesAKindWithoutWhatItNeedsFailsTheRunNamingTheSetting(string componentType, string kind, string setting)
    {
        var builder = componentType == "ContainerDestination"
            ? (FlowComponentBuilderBase)new ContainerDestinationFlowComponentBuilder()
            : new DataSetDestinationFlowComponentBuilder();

        var built = builder.Create(Destination(componentType, kind), ContextOn(new GraphPipelineConfiguration { Name = "Export" }, new Mock<Fdw.Services.Data.Abstractions.IDataGateway>().Object));

        built.IsFailure.ShouldBeTrue();
        string.Concat(built.CurrentMessage).ShouldContain(setting);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphThatSavesAnIncompleteDestinationOrAKindOnAnotherComponentIsRefused()
    {
        var graph = Graph([DataSetSource("src"), Destination("ContainerDestination", "Files"), DataSetSource("other")], [Link("src", "players")]);
        graph.Components[1].Columns.Add(new PipelineComponentColumnConfiguration { SettingName = "writeColumns", Ordinal = 0, ColumnName = "Name" });
        graph.Components[2].SplitField = "Team";

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance).Select(problem => string.Join("/", problem.Path)).ToList();

        problems.ShouldContain(path => path.Contains("players", StringComparison.Ordinal) && path.EndsWith("splitField", StringComparison.Ordinal));
        problems.ShouldContain(path => path.Contains("other", StringComparison.Ordinal) && path.EndsWith("splitField", StringComparison.Ordinal));
    }
}
