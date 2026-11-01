using Fdw.Data.PostgreSql;

namespace Fdw.Services.Connections.PostgreSql.Tests;

/// <summary>FDW-1967: a PostgreSQL path is read at another place by replacing its schema and/or its table; a part not stated is kept.</summary>
public sealed class PostgreSqlDatabasePathReaddressTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ASchemaAndATableAreReplacedAndTheOtherIsKept()
    {
        var declared = new PostgreSqlDatabasePath(null, "public", "orders");

        var table = declared.Readdress(null, "orders_2026").ShouldBeOfType<PostgreSqlDatabasePath>();
        var schema = declared.Readdress("archive", null).ShouldBeOfType<PostgreSqlDatabasePath>();

        table.QuotedIdentifier.ShouldBe("\"public\".\"orders_2026\"");
        schema.QuotedIdentifier.ShouldBe("\"archive\".\"orders\"");
        declared.ObjectName.ShouldBe("orders");
    }
}
