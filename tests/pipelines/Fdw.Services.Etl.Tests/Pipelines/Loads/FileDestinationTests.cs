using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Pipelines.Loads.LoadTestDoubles;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for a destination that writes one file, or one file for each value of a field (FDW-3193): each file is the declared container at its own address,
/// written in one unit of work with the others, and a run with a value that cannot name a file writes nothing.
/// </summary>
public sealed class FileDestinationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 9, 10, 11, TimeSpan.Zero);

    private readonly Guid _exports = Guid.NewGuid();
    private readonly List<string> _events = [];

    private static LoadAddressing OneFile(string name) => new(TargetKinds.File, null, name, null);

    private static LoadAddressing ByField(string field) => new(TargetKinds.Files, null, null, field);

    private static LoadUnit Unit(string kind, LoadAddressing? addressing)
    {
        var step = new PipelineLoadStepConfiguration
        {
            Name = "load",
            Kind = kind,
            IsEnabled = true,
            Columns = kind == "Insert"
                ? [new PipelineLoadColumnConfiguration { Name = "Team", Role = "Write", ColumnName = "Team", Ordinal = 0 }, new PipelineLoadColumnConfiguration { Name = "Name", Role = "Write", ColumnName = "Name", Ordinal = 1 }]
                : [],
        };
        return LoadUnit.ForContainer("graph destination 'players'", LoadStepKinds.ByName(kind), step, "Exports", "exports", "Players").WithAddressing(addressing);
    }

    private static List<IDictionary<string, object?>> Players(params (string? Team, string Name)[] players) =>
        players.Select(player => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Team"] = player.Team, ["Name"] = player.Name }).ToList();

    private static List<string> Names(IDataCommand command) =>
        ((InsertCommand<IDictionary<string, object?>>)command).Data.Select(row => (string)row["Name"]!).ToList();

    private (PipelineLoadExecutor Executor, RecordingConnection Connection) Executor(bool file = true)
    {
        var connection = new RecordingConnection("exports", _events);
        var dataStores = DataStoreProviderOf(("Exports", "exports", "Players", file ? FileContainer("Players", _exports, "exports/Players.csv") : Container("Players", _exports)));
        var types = new TypeResolver(new Dictionary<Guid, IConnectionType> { [_exports] = ConnectionType(AllCapabilities) });
        return (
            new PipelineLoadExecutor(new LoadTargetResolver(null, dataStores.Object), ConnectionProviderOf((_exports, connection.Connection)).Object, types, NullLogger.Instance),
            connection);
    }

    private static async Task<IGenericResult<int>> Run(PipelineLoadExecutor executor, LoadUnit[] units, List<IDictionary<string, object?>> rows) =>
        await executor.Run("export", units, rows, new Dictionary<string, object?>(), Now, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheFileKindWritesEveryRowToTheOneFileItNames()
    {
        var (executor, connection) = Executor();

        var result = await Run(executor, [Unit("Insert", OneFile("daily.csv"))], Players(("Chiefs", "Mahomes"), ("Broncos", "Nix")));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _events.ShouldBe(["begin:exports", "exec:exports:Insert", "commit:exports"]);
        connection.Targets.ShouldBe(["exports/daily.csv"]);
        Names(connection.Commands.Single()).ShouldBe(["Mahomes", "Nix"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheFilesKindWritesOneFilePerValueOfTheFieldAndEachHoldsOnlyItsRows()
    {
        var (executor, connection) = Executor();

        var result = await Run(executor, [Unit("Insert", ByField("Team"))], Players(("Chiefs", "Mahomes"), ("Broncos", "Nix"), ("Chiefs", "Kelce")));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _events.ShouldBe(["begin:exports", "exec:exports:Insert", "exec:exports:Insert", "commit:exports"]);
        connection.Targets.ShouldBe(["exports/Chiefs.csv", "exports/Broncos.csv"]);
        Names(connection.Commands[0]).ShouldBe(["Mahomes", "Kelce"]);
        Names(connection.Commands[1]).ShouldBe(["Nix"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheFilesKindEmptiesEachFileItWritesInTheSameUnitOfWork()
    {
        var (executor, connection) = Executor();
        var addressing = ByField("Team");

        var result = await Run(executor, [Unit("Truncate", addressing), Unit("Insert", addressing)], Players(("Chiefs", "Mahomes"), ("Broncos", "Nix")));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _events.ShouldBe(["begin:exports", "exec:exports:Delete", "exec:exports:Delete", "exec:exports:Insert", "exec:exports:Insert", "commit:exports"]);
        connection.Targets.ShouldBe(["exports/Chiefs.csv", "exports/Broncos.csv", "exports/Chiefs.csv", "exports/Broncos.csv"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADestinationThatStatesNoAddressingWritesTheContainerAsDeclared()
    {
        var (executor, connection) = Executor();

        var result = await Run(executor, [Unit("Insert", null)], Players(("Chiefs", "Mahomes")));

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        connection.Targets.ShouldBe(["exports/Players.csv"]);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Squad", "Squad")]
    [InlineData(null, "row 2")]
    [InlineData("", "row 2")]
    [InlineData("../Secrets", "..")]
    [InlineData("a/b", "'/'")]
    [InlineData("Chiefs ", "space")]
    [InlineData(".hidden", "dot")]
    [InlineData("chiefs", "differ only in case")]
    public async Task AFilesRunWithAValueThatCannotNameAFileWritesNothingAndNamesTheValue(string? bad, string expected)
    {
        var (executor, connection) = Executor();
        var field = bad == "Squad" ? "Squad" : "Team";

        var result = await Run(executor, [Unit("Insert", ByField(field))], Players(("Chiefs", "Mahomes"), (bad == "Squad" ? "Chiefs" : bad, "Kelce"), ("Broncos", "Nix")));

        result.IsFailure.ShouldBeTrue();
        string.Concat(result.CurrentMessage).ShouldContain(expected);
        _events.ShouldBeEmpty();
        connection.Targets.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ADestinationWhoseContainerIsNotAFileIsRefusedForTheFileAndFilesKinds(bool files)
    {
        var (executor, _) = Executor(file: false);

        var result = await Run(executor, [Unit("Insert", files ? ByField("Team") : OneFile("daily.csv"))], Players(("Chiefs", "Mahomes")));

        result.IsFailure.ShouldBeTrue();
        string.Concat(result.CurrentMessage).ShouldContain("is not a file");
        _events.ShouldBeEmpty();
    }
}
