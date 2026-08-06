using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Schema.Properties;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlFindTranslatorTests
{
    private readonly PostgreSqlFindTranslator _sut = new();

    private static IStorageContainer CreateMockContainer(
        string schema = "public",
        string tableName = "customers",
        IReadOnlyList<IField>? fields = null)
    {
        var dbPath = new PostgreSqlDatabasePath(null, schema, tableName);

        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns(fields ?? Array.Empty<IField>());

        var mockContainer = new Mock<IStorageContainer>();
        mockContainer.Setup(c => c.Name).Returns(tableName);
        mockContainer.Setup(c => c.Path).Returns(dbPath);
        mockContainer.Setup(c => c.Schema).Returns(mockSchema.Object);

        return mockContainer.Object;
    }

    private static IField CreateMockField(string name, Type clrType)
    {
        var mockFieldType = new Mock<IFieldType>();
        mockFieldType.Setup(ft => ft.ClrType).Returns(clrType);

        var mockField = new Mock<IField>();
        mockField.Setup(f => f.Name).Returns(name);
        mockField.Setup(f => f.FieldType).Returns(mockFieldType.Object);

        return mockField.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ConstructorSetsName()
    {
        _sut.Name.ShouldBe("Find");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateGeneratesILikeForCaseInsensitiveSearch()
    {
        // Why: PG uses ILIKE (not LIKE + COLLATE) for case-insensitive search.
        // Regression guard: do NOT emit "COLLATE Latin1_General_CS_AS".
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("name", typeof(string))
        });
        var command = new FindCommand<object> { SearchTerm = "acme", CaseSensitive = false };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ILIKE @searchTerm");
        result.Value.CommandText.ShouldNotContain("COLLATE");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateGeneratesLikeForCaseSensitiveSearch()
    {
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("name", typeof(string))
        });
        var command = new FindCommand<object> { SearchTerm = "Acme", CaseSensitive = true };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("LIKE @searchTerm");
        result.Value.CommandText.ShouldNotContain("ILIKE");
        result.Value.CommandText.ShouldNotContain("COLLATE");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateFromClauseUsesDoubleQuotedSchemaAndTable()
    {
        // Why: regression guard — FROM must emit "sales"."orders", not [sales].[orders].
        var container = CreateMockContainer(schema: "sales", tableName: "orders", fields: new[]
        {
            CreateMockField("name", typeof(string))
        });
        var command = new FindCommand<object> { SearchTerm = "test" };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("\"sales\".\"orders\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsNoRowsWhenNoSearchableColumnsUsesFALSE()
    {
        // Why: PG always-false predicate is "FALSE" not "1 = 0". Guards the dialect seam
        // for the no-searchable-columns path.
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("id", typeof(int)),
            CreateMockField("amount", typeof(decimal))
        });
        var command = new FindCommand<object> { SearchTerm = "test" };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE FALSE");
        result.Value.CommandText.ShouldNotContain("1 = 0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateIncludesOnlySpecifiedFieldsWhenInFieldsSet()
    {
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("name", typeof(string)),
            CreateMockField("email", typeof(string)),
            CreateMockField("phone", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "test",
            FieldNames = new[] { "name", "email" }
        };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        var whereClause = sql.Substring(sql.IndexOf("WHERE", StringComparison.Ordinal));
        whereClause.ShouldContain("\"name\"");
        whereClause.ShouldContain("\"email\"");
        whereClause.ShouldNotContain("\"phone\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateDefaultsToStringFieldsWhenNoFieldNamesSpecified()
    {
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("name", typeof(string)),
            CreateMockField("age", typeof(int)),
            CreateMockField("email", typeof(string))
        });
        var command = new FindCommand<object> { SearchTerm = "test" };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldContain("\"name\"");
        sql.ShouldContain("\"email\"");
        var whereClause = sql.Substring(sql.IndexOf("WHERE", StringComparison.Ordinal));
        whereClause.ShouldNotContain("\"age\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateGeneratesOrJoinedWhereForMultipleFields()
    {
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("name", typeof(string)),
            CreateMockField("description", typeof(string)),
            CreateMockField("email", typeof(string))
        });
        var command = new FindCommand<object> { SearchTerm = "test" };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldContain("\"name\"");
        sql.ShouldContain("\"description\"");
        sql.ShouldContain("\"email\"");
        sql.ShouldContain(" OR ");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsFailureForEmptySearchTerm()
    {
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("name", typeof(string))
        });
        var command = new FindCommand<object> { SearchTerm = "" };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateAddMaxResultsLimitClause()
    {
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("name", typeof(string))
        });
        var command = new FindCommand<object> { SearchTerm = "acme", MaxResults = 25 };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("LIMIT 25");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateOmitsLimitClauseWhenMaxResultsNotSet()
    {
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("name", typeof(string))
        });
        var command = new FindCommand<object> { SearchTerm = "acme" };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("LIMIT");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateDoesNotEmitTopClause()
    {
        // Why: TOP is T-SQL; PG uses LIMIT. Guard against T-SQL leaking through.
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("name", typeof(string))
        });
        var command = new FindCommand<object> { SearchTerm = "acme", MaxResults = 10 };

        var result = await _sut.Translate(command, container, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("TOP");
    }
}
