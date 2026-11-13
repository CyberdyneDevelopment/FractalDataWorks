using System.Linq;
using Fdw.Data.MsSql;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Data.MsSql.Tests.Scripts;

/// <summary>
/// FDW-5457: the T-SQL reader parses a script with ScriptDom and names, from the parse tree, the tables it reads, writes and creates, leaving out
/// what the script holds for itself.
/// </summary>
public sealed class MsSqlScriptReaderTests
{
    private const string Marker = "__fdw_run_time_name_0__";

    private static ScriptReading Read(string script, params string[] runTimeNames) => new MsSqlScriptReader().Read(script, runTimeNames);

    private static string[] Names(System.Collections.Generic.IReadOnlyList<ScriptObjectName> names) => [.. names.Select(name => name.Display)];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheReaderIsFoundByTheKindOfItsDataStore()
    {
        ScriptReaders.ByName("MsSql").ShouldBeOfType<MsSqlScriptReader>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AnInsertFromASelectWritesItsTargetAndReadsItsSources()
    {
        var reading = Read("INSERT INTO dbo.Game (Id) SELECT t.Id FROM dbo.Team t JOIN stage.Player p ON p.TeamId = t.Id WHERE p.Year = @Year");

        reading.Parsed.ShouldBeTrue();
        Names(reading.Writes).ShouldBe(["dbo.Game"]);
        Names(reading.Reads).ShouldBe(["dbo.Team", "stage.Player"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AnUpdateThatNamesItsTargetByAliasWritesTheAliasedTableAndDoesNotReadIt()
    {
        var reading = Read("UPDATE g SET g.Score = s.Score FROM dbo.Game g JOIN stage.Score s ON s.GameId = g.Id");

        Names(reading.Writes).ShouldBe(["dbo.Game"]);
        Names(reading.Reads).ShouldBe(["stage.Score"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ADeleteAMergeATruncateAndAnOutputIntoAreWrites()
    {
        var reading = Read("""
            DELETE FROM dbo.Old WHERE Id < 10;
            MERGE dbo.Target AS t USING stage.Source AS s ON t.Id = s.Id WHEN MATCHED THEN UPDATE SET t.Name = s.Name;
            TRUNCATE TABLE stage.Load;
            UPDATE dbo.Game SET Score = 0 OUTPUT inserted.Id INTO audit.Changed (Id);
            """);

        Names(reading.Writes).ShouldBe(["dbo.Old", "dbo.Target", "stage.Load", "dbo.Game", "audit.Changed"], ignoreOrder: true);
        Names(reading.Reads).ShouldBe(["stage.Source"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASelectIntoWritesAndCreatesItsTarget()
    {
        var reading = Read("SELECT Id INTO dbo.Snapshot FROM dbo.Team; CREATE TABLE dbo.Made (Id int);");

        Names(reading.Writes).ShouldBe(["dbo.Snapshot"]);
        Names(reading.Creates).ShouldBe(["dbo.Snapshot", "dbo.Made"]);
        Names(reading.Reads).ShouldBe(["dbo.Team"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TemporaryTablesTableVariablesAndCommonTableExpressionsAreTheScriptsOwn()
    {
        var reading = Read("""
            DECLARE @ids TABLE (Id int);
            INSERT INTO @ids SELECT Id FROM dbo.Team;
            SELECT Id INTO #work FROM @ids;
            WITH recent AS (SELECT Id FROM #work) INSERT INTO dbo.Game (Id) SELECT Id FROM recent;
            """);

        Names(reading.Reads).ShouldBe(["dbo.Team"]);
        Names(reading.Writes).ShouldBe(["dbo.Game"]);
        reading.Creates.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ANameWithNoSchemaHasNoPath()
    {
        Read("SELECT Id FROM Team").Reads.ShouldBe([new ScriptObjectName(null, "Team")]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ANameWithAPartGivenAtRunTimeIsListedApartAndNotAsARead()
    {
        var reading = Read($"SELECT Id FROM {Marker}.Team", Marker);

        reading.Reads.ShouldBeEmpty();
        reading.RunTimeNamed.ShouldBe([$"{Marker}.Team"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ANameInAnotherDatabaseIsOutsideTheStore()
    {
        var reading = Read("INSERT INTO dbo.Game (Id) SELECT Id FROM OtherDb.dbo.Team");

        reading.OutsideStore.ShouldBe(["OtherDb.dbo.Team"]);
        reading.Reads.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AScriptThatDoesNotParseSaysWhereAndNamesNothing()
    {
        var reading = Read("SELEC Id FROM dbo.Team");

        reading.Parsed.ShouldBeFalse();
        reading.Errors.ShouldNotBeEmpty();
        reading.Errors[0].Line.ShouldBe(1);
        reading.Reads.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ATableNamedTwiceIsListedOnce()
    {
        Names(Read("SELECT a.Id FROM dbo.Team a JOIN DBO.TEAM b ON a.Id = b.Id").Reads).ShouldBe(["dbo.Team"]);
    }
}
