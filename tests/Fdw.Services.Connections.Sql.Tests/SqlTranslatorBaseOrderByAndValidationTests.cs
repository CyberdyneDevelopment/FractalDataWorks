using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Sql.Tests.Fakes;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// Direct tests for SqlDataCommandTranslatorBase.BuildOrderByClause and IsValidColumnName
/// via the thin proxy. Asserts dialect-driven quoting, correct ASC/DESC keywords, and
/// injection-rejection across a variety of hostile column name strings.
/// </summary>
[Collection(nameof(SqlTranslatorTestCollection))]
public sealed class SqlTranslatorBaseOrderByAndValidationTests
{
    private static readonly FakeDialect Dialect = new();

    private static Mock<IOrderedField> OrderedField(string name, bool ascending)
    {
        var direction = ascending
            ? (ISortDirection)new AscendingDirection()
            : new DescendingDirection();

        var field = new Mock<IOrderedField>();
        field.Setup(f => f.PropertyName).Returns(name);
        field.Setup(f => f.Direction).Returns(direction);
        return field;
    }

    // ── BuildOrderByClause ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildOrderByClauseQuotesFieldNamesViaDialect()
    {
        // Why: ORDER BY column quoting must route through ISqlDialect.QuoteIdentifier —
        // not hardcoded bracket style. FakeDialect → "col" double-quote form.
        var ordering = new OrderingExpression
        {
            OrderedFields = [OrderedField("last_name", ascending: true).Object]
        };

        var result = SqlTranslatorProxy.ExposeOrderByClause(ordering, Dialect);

        result.ShouldBe("\"last_name\" ASC");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildOrderByClauseEmitsDescKeywordForDescendingDirection()
    {
        var ordering = new OrderingExpression
        {
            OrderedFields = [OrderedField("created_at", ascending: false).Object]
        };

        var result = SqlTranslatorProxy.ExposeOrderByClause(ordering, Dialect);

        result.ShouldBe("\"created_at\" DESC");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildOrderByClauseMultipleFieldsCommaSeparated()
    {
        var ordering = new OrderingExpression
        {
            OrderedFields =
            [
                OrderedField("last_name", ascending: true).Object,
                OrderedField("first_name", ascending: false).Object
            ]
        };

        var result = SqlTranslatorProxy.ExposeOrderByClause(ordering, Dialect);

        result.ShouldBe("\"last_name\" ASC, \"first_name\" DESC");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildOrderByClauseRejectsInjectionInFieldName()
    {
        // Why: BuildOrderByClause calls IsValidColumnName before quoting. An injection attempt
        // (semicolon, dash) must throw ArgumentException — not produce malformed SQL.
        var ordering = new OrderingExpression
        {
            OrderedFields = [OrderedField("a; DROP TABLE", ascending: true).Object]
        };

        Should.Throw<System.ArgumentException>(
            () => SqlTranslatorProxy.ExposeOrderByClause(ordering, Dialect));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildOrderByClauseRejectsDottedFieldName()
    {
        // Why: a dotted name (schema.column) is rejected in ORDER BY — the caller must
        // use a plain column alias, not a qualified name.
        var ordering = new OrderingExpression
        {
            OrderedFields = [OrderedField("table.column", ascending: true).Object]
        };

        Should.Throw<System.ArgumentException>(
            () => SqlTranslatorProxy.ExposeOrderByClause(ordering, Dialect));
    }

    // ── IsValidColumnName ───────────────────────────────────────────────────

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("status")]
    [InlineData("_internal")]
    [InlineData("col123")]
    [InlineData("CamelCase")]
    [InlineData("_")]
    public void IsValidColumnNameAcceptsLegalIdentifiers(string name)
    {
        SqlTranslatorProxy.ExposeIsValidColumnName(name).ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("a; DROP TABLE orders")]   // SQL injection attempt
    [InlineData("col name")]               // space
    [InlineData("table.column")]           // dotted — rejected by IsValidColumnName
    [InlineData("1starts_with_digit")]     // digit start
    [InlineData("")]                       // empty
    [InlineData("   ")]                    // whitespace only
    [InlineData("col-name")]              // hyphen
    public void IsValidColumnNameRejectsIllegalIdentifiers(string name)
    {
        SqlTranslatorProxy.ExposeIsValidColumnName(name).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsValidColumnNameUnderscoreStartIsAccepted()
    {
        // Why: underscores are common for internal/private columns (e.g., _RowId, _version).
        SqlTranslatorProxy.ExposeIsValidColumnName("_hidden").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsValidColumnNameDigitStartIsRejected()
    {
        SqlTranslatorProxy.ExposeIsValidColumnName("2fast").ShouldBeFalse();
    }
}
