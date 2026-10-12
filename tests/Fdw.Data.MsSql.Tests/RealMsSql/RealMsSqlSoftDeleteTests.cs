using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.MsSql.Tests.TestSupport;
using static Fdw.Data.MsSql.Tests.RealMsSql.RealMsSqlSupport;

namespace Fdw.Data.MsSql.Tests.RealMsSql;

/// <summary>Soft delete against a real SQL Server: rows are flagged, never removed.</summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class RealMsSqlSoftDeleteTests(RealMsSqlFixture fixture) : IClassFixture<RealMsSqlFixture>
{
    private static readonly TestContainers.Column[] Columns =
    [
        new("Id", true), new("Code"), new("Status"), new("IsDeleted"), new("DeletedAt"),
    ];

    private async Task<string> CreateAccounts()
    {
        var table = fixture.Unique("Account");
        await fixture.Exec(
            $"CREATE TABLE [{fixture.Schema}].[{table}] (Id int IDENTITY(1,1) PRIMARY KEY, Code nvarchar(20) NOT NULL, Status nvarchar(20) NULL, "
            + "IsDeleted bit NOT NULL DEFAULT 0, DeletedAt datetimeoffset(7) NULL); "
            + $"INSERT INTO [{fixture.Schema}].[{table}] (Code, Status) VALUES (N'A', N'open'), (N'B', N'closed'), (N'C', N'open'), (N'D', N'closed');");
        return table;
    }

    [Fact]
    public async Task MarkerMatchesFlagsTheMarkedRowsAndRemovesNothing()
    {
        fixture.RequireDatabase();
        var table = await CreateAccounts();
        var container = Container(fixture, table, Columns);
        var command = new DeleteCommand
        {
            SoftDelete = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches)
            {
                DeletedAtColumn = "DeletedAt",
                Marker = new SoftDeleteMarker("Status", "closed"),
            },
        };

        var affected = await Run(fixture, command, container.Object);

        affected.ShouldBe(2);
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{table}]")).ShouldBe(4);
        var flagged = await fixture.Rows($"SELECT Code, DeletedAt FROM [{fixture.Schema}].[{table}] WHERE IsDeleted = 1 ORDER BY Code");
        flagged.Select(r => (string)r["Code"]!).ToArray().ShouldBe(new[] { "B", "D" });
        flagged.ShouldAllBe(r => r["DeletedAt"] != null);
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{table}] WHERE IsDeleted = 0 AND DeletedAt IS NULL")).ShouldBe(2);
    }

    [Fact]
    public async Task AbsentFromSourceFlagsTheKeysThatDisappearedFromTheSource()
    {
        fixture.RequireDatabase();
        var table = await CreateAccounts();
        var container = Container(fixture, table, Columns);
        var present = new List<IReadOnlyDictionary<string, object?>>
        {
            Row(("Code", "A")),
            Row(("Code", "B")),
        };
        var command = new DeleteCommand
        {
            SoftDelete = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource)
            {
                DeletedAtColumn = "DeletedAt",
                AbsentFromSource = new AbsentKeySet(["Code"], present),
            },
        };

        var affected = await Run(fixture, command, container.Object);

        affected.ShouldBe(2);
        (await fixture.Scalar($"SELECT COUNT(*) FROM [{fixture.Schema}].[{table}]")).ShouldBe(4);
        var flagged = await fixture.Rows($"SELECT Code FROM [{fixture.Schema}].[{table}] WHERE IsDeleted = 1 ORDER BY Code");
        flagged.Select(r => (string)r["Code"]!).ToArray().ShouldBe(new[] { "C", "D" });
    }

    [Fact]
    public async Task FlaggingTwiceDoesNotReflagOrRecountTheSameRows()
    {
        fixture.RequireDatabase();
        var table = await CreateAccounts();
        var container = Container(fixture, table, Columns);
        var command = new DeleteCommand
        {
            SoftDelete = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches) { Marker = new SoftDeleteMarker("Status", "closed") },
        };

        await Run(fixture, command, container.Object);
        var again = await Run(fixture, command, container.Object);

        again.ShouldBe(0);
    }
}
