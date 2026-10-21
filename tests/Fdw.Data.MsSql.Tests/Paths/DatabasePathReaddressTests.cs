using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql.Tests.Paths;

/// <summary>
/// FDW-1967: a database path says how it is read at another place, because the SQL Server translators take the table from it: the path part
/// is the schema and the name part is the table, the database is kept, and a part not stated keeps this path's.
/// </summary>
public sealed class DatabasePathReaddressTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AStatedNameReplacesTheTableAndKeepsTheSchemaAndTheDatabase()
    {
        var moved = new DatabasePath("devDataDb", "BreweryData", "Brewery").Readdress(null, "BreweryMicro");

        var path = moved.ShouldBeOfType<DatabasePath>();
        path.Database.ShouldBe("devDataDb");
        path.Schema.ShouldBe("BreweryData");
        path.ObjectName.ShouldBe("BreweryMicro");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AStatedPathReplacesTheSchemaAndKeepsTheTable()
    {
        var path = new DatabasePath(null, "BreweryData", "Brewery").Readdress("Archive", " ").ShouldBeOfType<DatabasePath>();

        path.Schema.ShouldBe("Archive");
        path.ObjectName.ShouldBe("Brewery");
        path.QuotedIdentifier.ShouldBe("[Archive].[Brewery]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheDeclaredPathIsNotChanged()
    {
        var declared = new DatabasePath(null, "BreweryData", "Brewery");

        declared.Readdress("Archive", "Old");

        declared.Schema.ShouldBe("BreweryData");
        declared.ObjectName.ShouldBe("Brewery");
        declared.ShouldBeAssignableTo<IReaddressablePath>();
    }
}
