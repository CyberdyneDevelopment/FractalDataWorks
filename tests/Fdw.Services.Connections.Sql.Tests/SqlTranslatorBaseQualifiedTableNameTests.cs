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
        // Why: most connections don't include the database segment in the table name
        // (single-database connections where USE db is implicit).
        var path = new FakeDatabasePath(null, "public", "customers", SchemaAware);

        SqlTranslatorProxy.ExposeQualifiedTableName(path)
            .ShouldBe("\"public\".\"customers\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameWithSchemaAndDatabaseEmitsThreePart()
    {
        // Why: cross-database queries (T-SQL linked server, PG foreign data wrapper) emit a
        // three-part database.schema.object name.
        var path = new FakeDatabasePath("mydb", "dbo", "orders", SchemaAware);

        SqlTranslatorProxy.ExposeQualifiedTableName(path)
            .ShouldBe("\"mydb\".\"dbo\".\"orders\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameWithEmptyDatabaseStringSkipsDatabasePart()
    {
        // Why: empty string is treated the same as null — no database segment emitted.
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
        // Why: SQLite has no schema namespace. The schemaless path is the key branch this
        // test suite exists to guard — previously only exercised when SQLite is wired up.
        var path = new FakeDatabasePath(null, null, "events", Schemaless);

        SqlTranslatorProxy.ExposeQualifiedTableName(path)
            .ShouldBe("\"events\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameSchemalessDialectIgnoresNonNullSchemaField()
    {
        // Why: a schemaless dialect must ignore the Schema value even when it is populated,
        // because the SQL engine has no schema concept. This is the abstraction contract.
        var path = new FakeDatabasePath(null, "ignored_schema", "products", Schemaless);

        SqlTranslatorProxy.ExposeQualifiedTableName(path)
            .ShouldBe("\"products\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildQualifiedTableNameSchemalessDialectIgnoresDatabaseField()
    {
        // Why: schemaless dialects also ignore the Database segment; the abstraction must
        // not emit [db]."table" for SQLite.
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
        // Why: used for bulk-copy destinations where the database is implicit in the connection.
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
        // Why: the fake dialect uses double-quote quoting; proves quoting is dialect-routed
        // (not hardcoded to bracket or backtick style in the base).
        var path = new FakeDatabasePath(null, "myschema", "mytable", SchemaAware);

        var result = SqlTranslatorProxy.ExposeQualifiedTableName(path);

        result.ShouldContain("\"myschema\"");
        result.ShouldContain("\"mytable\"");
    }
}
