using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql.Tests.Scripts;

/// <summary>
/// FDW-5457: saving a graph judges each SQL step's script with the reader of its data store's kind. A save refuses what the check can prove wrong
/// (the script does not parse, a table the catalogued store does not hold, a schema-less name two schemas hold, a store that cannot be read) and warns
/// of what it cannot judge (no reader for the store's kind, names outside the store, a store not yet discovered). Each finding is its own coded
/// message, placed on the step's script setting.
/// </summary>
public sealed class GraphScriptProblemsTests
{
    private static readonly string[] ScriptPath = ["components", "load", "settings", "script"];

    private static IDataStoreImplementationConfiguration Store(string kind, params (string Path, string[] Containers)[] paths)
    {
        var store = new Mock<IDataStoreImplementationConfiguration>();
        store.SetupGet(s => s.Implementation).Returns(kind);
        store.SetupGet(s => s.Paths).Returns(paths
            .Select(p => new DataPathConfiguration
            {
                Name = p.Path,
                Containers = p.Containers.Select(name => new DataContainerConfiguration { Name = name }).ToList(),
            })
            .ToList());
        return store.Object;
    }

    private static IDataStoreImplementationConfiguration NflDb() => Store("MsSql", ("NflData", ["Team", "Game", "Player"]), ("stage", ["Player"]));

    private static GraphPipelineConfiguration Graph(string dataStore, string script) => new()
    {
        Name = "nfl",
        Components = [new PipelineComponentConfiguration { Name = "load", ComponentType = "ExecuteSql", DataStoreName = dataStore, ScriptText = script, Stage = 1 }],
    };

    private static Task<GraphFindings> Judge(GraphPipelineConfiguration graph, IDataStoreImplementationConfiguration store) =>
        GraphScriptProblems.Judge(
            graph,
            (_, _) => Found(store),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

    private static Task<IGenericResult<IDataStoreImplementationConfiguration>> Found(IDataStoreImplementationConfiguration store)
        => Task.FromResult<IGenericResult<IDataStoreImplementationConfiguration>>(GenericResult<IDataStoreImplementationConfiguration>.Success(store));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AScriptThatNamesOnlyTablesItsStoreHoldsHasNoFinding()
    {
        var findings = await Judge(Graph("NflDb", "INSERT INTO NflData.Game (Id) SELECT Id FROM NflData.Team WHERE Year = @Year"), NflDb());

        findings.Problems.ShouldBeEmpty();
        findings.Warnings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AScriptThatDoesNotParseIsRefusedWithWhereItFailed()
    {
        var findings = await Judge(Graph("NflDb", "INSERT INTO NflData.Game SELEC FROM"), NflDb());

        findings.Problems.Count.ShouldBe(1);
        findings.Problems[0].Path.ShouldBe(ScriptPath);
        findings.Problems[0].Reason.ShouldContain("does not parse");
        findings.Problems[0].Reason.ShouldContain("line 1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ATableTheCataloguedStoreDoesNotHoldIsRefusedByName()
    {
        var findings = await Judge(Graph("NflDb", "DELETE FROM NflData.Coach"), NflDb());

        findings.Problems.Count.ShouldBe(1);
        findings.Problems[0].Reason.ShouldContain("'NflData.Coach'");
        findings.Problems[0].Reason.ShouldContain("does not hold");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ATableWithNoSchemaThatTwoPathsHoldIsRefusedAsAmbiguous()
    {
        var findings = await Judge(Graph("NflDb", "SELECT Id FROM Player"), NflDb());

        findings.Problems.Count.ShouldBe(1);
        findings.Problems[0].Reason.ShouldContain("name the schema");
        findings.Problems[0].Reason.ShouldContain("'NflData'");
        findings.Problems[0].Reason.ShouldContain("'stage'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ATableWithNoSchemaThatOnePathHoldsIsFound()
    {
        (await Judge(Graph("NflDb", "SELECT Id FROM Team"), NflDb())).Problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ANameInAnotherDatabaseIsAWarningNotARefusal()
    {
        var findings = await Judge(Graph("NflDb", "INSERT INTO NflData.Game (Id) SELECT Id FROM OtherDb.dbo.Team"), NflDb());

        findings.Problems.ShouldBeEmpty();
        findings.Warnings.Count.ShouldBe(1);
        findings.Warnings[0].Path.ShouldBe(ScriptPath);
        findings.Warnings[0].Reason.ShouldContain("outside data store 'NflDb'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ATableTheScriptCreatesNeedNotExistYet()
    {
        var script = "SELECT Id INTO NflData.Snapshot FROM NflData.Team; INSERT INTO NflData.Snapshot (Id) SELECT Id FROM NflData.Game;";

        (await Judge(Graph("NflDb", script), NflDb())).Problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ANameGivenAtRunTimeIsNotJudgedBeforeTheRun()
    {
        var findings = await Judge(Graph("NflDb", "DELETE FROM $(Schema).Whatever WHERE Id = @Id"), NflDb());

        findings.Problems.ShouldBeEmpty();
        findings.Warnings.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStoreWhoseKindHasNoReaderIsAWarningAndSavesAsBefore()
    {
        var findings = await Judge(Graph("Files", "this is not checked"), Store("PostgreSql", ("public", ["Team"])));

        findings.Problems.ShouldBeEmpty();
        findings.Warnings.Count.ShouldBe(1);
        findings.Warnings[0].Reason.ShouldContain("whose scripts this host cannot read");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStoreWithNoCatalogueIsAWarningAndItsTablesAreNotChecked()
    {
        var findings = await Judge(Graph("NewDb", "DELETE FROM dbo.Anything"), Store("MsSql", ("dbo", [])));

        findings.Problems.ShouldBeEmpty();
        findings.Warnings.Count.ShouldBe(1);
        findings.Warnings[0].Reason.ShouldContain("has no catalogue");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AScriptThatDoesNotParseIsRefusedEvenOnAStoreWithNoCatalogue()
    {
        var findings = await Judge(Graph("NewDb", "DELET FROM dbo.Anything"), Store("MsSql"));

        findings.Problems.Count.ShouldBe(1);
        findings.Problems[0].Reason.ShouldContain("does not parse");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStoreThatCannotBeReadIsRefused()
    {
        var findings = await GraphScriptProblems.Judge(
            Graph("Gone", "SELECT Id FROM Team"),
            (_, _) => Task.FromResult<IGenericResult<IDataStoreImplementationConfiguration>>(
                GenericResult<IDataStoreImplementationConfiguration>.Failure(ServicesResultCodes.ByName("ConfigurationNotFound"))),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        findings.Problems.Count.ShouldBe(1);
        findings.Problems[0].Reason.ShouldContain("data store 'Gone', which cannot be read");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStoreGivenAtRunTimeIsNotRead()
    {
        var reads = 0;
        var findings = await GraphScriptProblems.Judge(
            Graph("{{Store}}", "SELECT Id FROM Team"),
            (_, _) =>
            {
                reads++;
                return Found(NflDb());
            },
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

        findings.Problems.ShouldBeEmpty();
        findings.Warnings.ShouldBeEmpty();
        reads.ShouldBe(0);
    }
}
