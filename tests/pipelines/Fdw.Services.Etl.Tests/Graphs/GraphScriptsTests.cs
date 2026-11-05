using System.Linq;
using Fdw.Services.Etl.Graphs;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for finding the script text a stored graph holds (FDW-3676): a SQL source and an Execute SQL step with text hold it, and nothing else does.
/// </summary>
public sealed class GraphScriptsTests
{
    private static PipelineComponentConfiguration SqlSource(string name, string? script) =>
        new() { Name = name, ComponentType = "SqlSource", DataStoreName = "Warehouse", ScriptText = script, MaxRows = 10, BatchSize = 2 };

    private static PipelineComponentConfiguration ExecuteSql(string name, string? script) =>
        new() { Name = name, ComponentType = "ExecuteSql", DataStoreName = "Warehouse", ScriptText = script, Stage = 1 };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASqlSourceAndAnExecuteSqlStepWithTextHoldAScript()
    {
        var graph = Graph([SqlSource("read", "SELECT Id FROM dbo.T"), DataSetSource("plain"), ExecuteSql("purge", "DELETE FROM dbo.T")], []);

        GraphScripts.ComponentsHolding(graph).ShouldBe(["read", "purge"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AScriptComponentWithNoTextOrOnlySpacesHoldsNoScript()
    {
        var graph = Graph([SqlSource("empty", null), SqlSource("blank", "   "), ExecuteSql("none", string.Empty)], []);

        GraphScripts.ComponentsHolding(graph).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AGraphOfComponentsThatTakeNoScriptHoldsNone()
    {
        var graph = Graph([DataSetSource("src"), Mapped("map", ("A", "B"))], [Link("src", "map")]);

        GraphScripts.ComponentsHolding(graph).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void EveryKindWithASettingOfKindSqlTextIsFoundWithoutBeingNamedHere()
    {
        var kinds = Fdw.Services.Etl.Abstractions.Flow.ComponentTypes.All()
            .Where(kind => kind.Settings.Any(setting => setting.Kind == GraphScripts.SettingKind))
            .Select(kind => kind.Name)
            .ToList();

        kinds.ShouldBe(["ExecuteSql", "SqlSource"], ignoreOrder: true);
    }
}
