using System.Linq;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-5290: a join's key must be a field its input maps; an input that states no mapping passes every field.</summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5290")]
public sealed class GraphDataSetJoinKeysTests
{
    private static PipelineComponentConfiguration Join(string kind = "MergeJoin")
    {
        var join = new PipelineComponentConfiguration { Name = "Join1", ComponentType = kind };
        join.Columns.Add(new PipelineComponentColumnConfiguration { SettingName = "leftKeys", Ordinal = 0, ColumnName = "CustomerId" });
        join.Columns.Add(new PipelineComponentColumnConfiguration { SettingName = "rightKeys", Ordinal = 0, ColumnName = "Id" });
        return join;
    }

    private static void Map(PipelineComponentConfiguration join, string input, string target) =>
        join.Fields.Add(new PipelineComponentFieldConfiguration { SettingName = "mappings", InputName = input, TargetField = target, SourceField = target });

    private static string[] KeysReported(PipelineComponentConfiguration join) =>
    [
        .. GraphDataSetJoinKeys.Problems(new GraphPipelineConfiguration { Components = [join] }, "Revenue", NullLogger.Instance)
            .Select(problem => string.Join('/', problem.Path)),
    ];

    [Fact]
    public void AKeyTheLeftInputDoesNotMapIsReportedAtItsSetting()
    {
        var join = Join();
        Map(join, "left", "OrderId");

        var problems = GraphDataSetJoinKeys.Problems(new GraphPipelineConfiguration { Components = [join] }, "Revenue", NullLogger.Instance).ToList();

        var problem = problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(DataServiceResultCodes.ByName("GraphDataSetJoinFieldUnmapped").Code);
        string.Join('/', problem.Path).ShouldBe("components/Join1/settings/leftKeys");
        problem.Message.ShouldContain("CustomerId");
    }

    [Fact]
    public void AKeyTheInputMapsIsNotReportedWhateverItsCase()
    {
        var join = Join();
        Map(join, "left", "customerid");
        Map(join, "right", "Id");

        KeysReported(join).ShouldBeEmpty();
    }

    [Fact]
    public void AnInputThatStatesNoMappingPassesEveryField()
    {
        KeysReported(Join()).ShouldBeEmpty();
    }

    [Fact]
    public void EachSideIsJudgedAgainstItsOwnInput()
    {
        var join = Join();
        Map(join, "left", "CustomerId");
        Map(join, "right", "Other");

        KeysReported(join).ShouldBe(new[] { "components/Join1/settings/rightKeys" });
    }

    [Fact]
    public void AComponentThatIsNotAJoinIsNotJudged()
    {
        var component = Join("Lookup");
        Map(component, "left", "OrderId");

        KeysReported(component).ShouldBeEmpty();
    }

    [Fact]
    public void TheNamesTheRuleReadsAreTheOnesTheMergeJoinDeclares()
    {
        var declared = ComponentTypes.ByName(GraphDataSetJoinKeys.MergeJoin);

        declared.Name.ShouldBe(GraphDataSetJoinKeys.MergeJoin);
        foreach (var (setting, input) in GraphDataSetJoinKeys.KeySettings)
        {
            declared.Settings.Select(member => member.Name).ShouldContain(setting);
            declared.Inputs.Select(node => node.Name).ShouldContain(input);
        }
    }
}
