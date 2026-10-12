using Fdw.Services.Connections.Sql.Tests.Fakes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// Direct tests for SqlDataCommandTranslatorBase.BuildQualifiedTableName via the thin proxy.
/// Covers the schema-aware branch (SupportsSchemaNamespace=true) with and without a database
/// prefix, AND the schemaless branch (SupportsSchemaNamespace=false) — the SQLite path that
/// is otherwise only exercised once a SQLite dialect exists.
/// </summary>
[Collection(nameof(SqlTranslatorTestCollection))]
public sealed class SqlTranslatorBaseQualifiedTableNameTests
{
    private static readonly FakeDialect SchemaAware = new(supportsSchemaNamespace: true);
    private static readonly FakeDialect Schemaless = new(supportsSchemaNamespace: false);

    // ── Schema-aware dialect ─────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameWithSchemaAndNoDatabaseEmitsSchemaAndObject()
    {
        var path = new FakeDatabasePath(null, "public", "customers", SchemaAware);

        SqlTranslatorProxy.ExposeQualifiedTableName(path)
            .ShouldBe("\"public\".\"customers\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameWithSchemaAndDatabaseEmitsThreePart()
    {
        var path = new FakeDatabasePath("mydb", "dbo", "orders", SchemaAware);

        SqlTranslatorProxy.ExposeQualifiedTableName(path)
            .ShouldBe("\"mydb\".\"dbo\".\"orders\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameWithEmptyDatabaseStringSkipsDatabasePart()
    {
        var path = new FakeDatabasePath(string.Empty, "app", "users", SchemaAware);

        SqlTranslatorProxy.ExposeQualifiedTableName(path)
            .ShouldBe("\"app\".\"users\"");
    }

    // ── Schemaless dialect (the SQLite path) ────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameSchemalessDialectEmitsBareQuotedObject()
    {
        var path = new FakeDatabasePath(null, null, "events", Schemaless);

        SqlTranslatorProxy.ExposeQualifiedTableName(path)
            .ShouldBe("\"events\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameSchemalessDialectIgnoresNonNullSchemaField()
    {
        var path = new FakeDatabasePath(null, "ignored_schema", "products", Schemaless);

        SqlTranslatorProxy.ExposeQualifiedTableName(path)
            .ShouldBe("\"products\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameSchemalessDialectIgnoresDatabaseField()
    {
        var path = new FakeDatabasePath("ignored_db", "ignored_schema", "logs", Schemaless);

        SqlTranslatorProxy.ExposeQualifiedTableName(path)
            .ShouldBe("\"logs\"");
    }

    // ── BuildSchemaQualifiedTableName (no database prefix) ─────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildSchemaQualifiedTableNameOmitsDatabaseSegment()
    {
        var path = new FakeDatabasePath("mydb", "staging", "imports", SchemaAware);

        SqlTranslatorProxy.ExposeSchemaQualifiedTableName(path)
            .ShouldBe("\"staging\".\"imports\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildSchemaQualifiedTableNameSchemalessDialectEmitsBareObject()
    {
        var path = new FakeDatabasePath(null, null, "raw_data", Schemaless);

        SqlTranslatorProxy.ExposeSchemaQualifiedTableName(path)
            .ShouldBe("\"raw_data\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameQuotesIdentifiersViaDialect()
    {
        var path = new FakeDatabasePath(null, "myschema", "mytable", SchemaAware);

        var result = SqlTranslatorProxy.ExposeQualifiedTableName(path);

        result.ShouldContain("\"myschema\"");
        result.ShouldContain("\"mytable\"");
    }
}
