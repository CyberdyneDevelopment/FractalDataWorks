using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for what a container source reads (FDW-3089): the target kinds Container, File and Files, the settings each needs and refuses judged in one
/// place for the graph validator and the builder, and a list of files built from a list run parameter, with nothing assumed.
/// </summary>
public sealed class TargetKindTests
{
    private static PipelineComponentConfiguration Source(string? kind, string? physicalPath = null, string? physicalName = null, string? filesList = null) => new()
    {
        Name = "files",
        ComponentType = "ContainerSource",
        DataStoreName = "Landing",
        PathName = "inbound",
        ContainerName = "Orders",
        BatchSize = 50,
        TargetKind = kind,
        PhysicalPath = physicalPath,
        PhysicalName = physicalName,
        FilesList = filesList,
    };

    private static FlowBuildContext Context(IDataGateway gateway, params (string Name, object? Value)[] parameters) =>
        ContextOn(new GraphPipelineConfiguration { Name = "Ingest" }, gateway, parameters.ToDictionary(pair => pair.Name, pair => pair.Value));

    private static IDataGateway Etl() => new Mock<IEtlDataGateway>().Object;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheKindsAreContainerFileAndFilesAndSayWhatTheyNeedAndRefuse()
    {
        TargetKinds.All().Select(kind => kind.Name).Order(StringComparer.Ordinal).ShouldBe(["Container", "DataSet", "File", "Files"]);
        TargetKinds.ByName("File").Needs.ShouldBe(["physicalName"]);
        TargetKinds.ByName("Files").Needs.ShouldBe(["filesList"]);
        TargetKinds.ByName("Files").Refuses.ShouldBe(["physicalName", "splitField"]);
        TargetKinds.ByName("Container").Refuses.ShouldBe(["physicalPath", "physicalName", "filesList", "splitField"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheKindAndTheListAreInTheCatalogueAsAPickerAndATextAndTheBindingTableReadsAndWritesThem()
    {
        var settings = ComponentTypes.ByName("ContainerSource").Settings;
        settings.Single(setting => setting.Name == "targetKind").Kind.ShouldBe("TargetKinds");
        settings.Single(setting => setting.Name == "filesList").Kind.ShouldBe("Text");
        var component = Source(null);

        ComponentSettingBindings.Find("targetKind")!.Set(component, "Files");
        ComponentSettingBindings.Find("filesList")!.Set(component, "Names");

        component.TargetKind.ShouldBe("Files");
        component.FilesList.ShouldBe("Names");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null, null, null, null, 0, "")]
    [InlineData(null, null, "orders.csv", null, 0, "")]
    [InlineData("Container", null, null, null, 0, "")]
    [InlineData("File", "inbound/x", "orders.csv", null, 0, "")]
    [InlineData("Files", "inbound/x", null, "Names", 0, "")]
    [InlineData(null, null, null, "Names", 1, "filesList")]
    [InlineData("Folder", null, null, null, 1, "targetKind")]
    [InlineData("File", null, null, null, 1, "physicalName")]
    [InlineData("Files", null, null, null, 1, "filesList")]
    [InlineData("Files", null, "orders.csv", "Names", 1, "physicalName")]
    [InlineData("File", null, "orders.csv", "Names", 1, "filesList")]
    [InlineData("Container", null, "orders.csv", null, 1, "physicalName")]
    public void ASourceIsJudgedByTheKindItNames(string? kind, string? physicalPath, string? physicalName, string? filesList, int problems, string field)
    {
        var found = TargetKindRules.Problems(Source(kind, physicalPath, physicalName, filesList));

        found.Count.ShouldBe(problems);
        if (problems > 0)
        {
            found.Single().Field.ShouldBe(field);
            found.Single().Message.ShouldContain("files");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AListOfFilesIsBuiltFromAListRunParameterAndNeedsTheEtlDataGateway()
    {
        var builder = new ContainerSourceFlowComponentBuilder();

        var built = builder.Create(Source("Files", filesList: "Names"), Context(Etl(), ("Names", new List<string> { "a.csv", "b.csv" })));
        var plain = builder.Create(Source("Files", filesList: "Names"), Context(new Mock<IDataGateway>().Object, ("Names", new List<string> { "a.csv" })));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        plain.IsFailure.ShouldBeTrue();
        plain.CurrentMessage.ShouldNotBeNull().ShouldContain("ETL");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("missing", "not a run parameter")]
    [InlineData("text", "not a list")]
    [InlineData("empty", "is empty")]
    [InlineData("blank", "not a file name")]
    public void AListThatIsMissingTextEmptyOrHasABlankItemFailsTheRunNamingTheListAndIsNeverAListOfOne(string held, string reason)
    {
        var parameters = held switch
        {
            "text" => new[] { ("Names", (object?)"a.csv") },
            "empty" => new[] { ("Names", (object?)new List<string>()) },
            "blank" => new[] { ("Names", (object?)new List<string> { "a.csv", " " }) },
            _ => [],
        };

        var built = new ContainerSourceFlowComponentBuilder().Create(Source("Files", filesList: "Names"), Context(Etl(), parameters));

        built.IsFailure.ShouldBeTrue();
        var message = built.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("Names");
        message.ShouldContain(reason);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASourceThatStatesAKindWithoutWhatItNeedsFailsTheRunNamingTheSetting()
    {
        var built = new ContainerSourceFlowComponentBuilder().Create(Source("File"), Context(Etl()));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("physicalName");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ASourceThatStatesNoKindIsBuiltAsItWasBeforeTheKindsExisted()
    {
        var built = new ContainerSourceFlowComponentBuilder().Create(Source(null), Context(new Mock<IDataGateway>().Object));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphThatSavesAKindWithoutWhatItNeedsOrAKindOnAnotherComponentIsRefused()
    {
        var graph = Graph([Source("Files"), DataSetSource("other")], []);
        graph.Components[1].TargetKind = "File";

        var problems = GraphShapeValidator.Problems(graph, NullLogger.Instance).Select(problem => string.Join("/", problem.Path)).ToList();

        problems.ShouldContain(path => path.EndsWith("filesList", StringComparison.Ordinal));
        problems.ShouldContain(path => path.EndsWith("targetKind", StringComparison.Ordinal));
    }
}
