using System;
using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Sql.Tests.Fakes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// Direct tests for SqlDataCommandTranslatorBase.BuildWhereClause via the thin proxy.
/// Asserts that quoting routes through the dialect, params use the dialect's prefix, empty-IN
/// uses AlwaysFalsePredicate, and the addParam delegate receives the correct (name, value) pairs.
/// These tests are dialect-independent — all assertions use the FakeDialect ("col") form.
/// </summary>
[Collection(nameof(SqlTranslatorTestCollection))]
public sealed class SqlTranslatorBaseWhereClauseTests
{
    private static readonly FakeDialect Dialect = new();

    private static (string sql, List<(string name, object? value)> captured) BuildWhere(
        IFilterExpression filter,
        string parameterPrefix = "@")
    {
        var captured = new List<(string, object?)>();
        var sql = SqlTranslatorProxy.ExposeWhereClause(
            filter,
            Dialect,
            (name, value) => captured.Add((name, value)),
            parameterPrefix: parameterPrefix);
        return (sql, captured);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildWhereClauseQuotesColumnViaDialect()
    {
        // Why: column quoting must go through ISqlDialect.QuoteIdentifier — no hardcoded
        // bracket or backtick style in the shared base. FakeDialect uses double-quote form.
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "status",
                Operator = new EqualOperator(),
                Value = "active"
            }
        };

        var (sql, _) = BuildWhere(filter);

        sql.ShouldContain("\"status\"");
        sql.ShouldNotContain("[status]");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildWhereClauseInvokesAddParamWithNameWithoutAtSign()
    {
        // Why: the addParam delegate receives the key WITHOUT the leading @ marker so the
        // backend can prepend it (SqlParameter("@name", value)). The @ appears in SQL text
        // only — never in the key passed to addParam.
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "id",
                Operator = new EqualOperator(),
                Value = 42
            }
        };

        var (sql, captured) = BuildWhere(filter);

        captured.Count.ShouldBe(1);
        captured[0].name.ShouldBe("p0");    // key without @
        captured[0].value.ShouldBe(42);
        sql.ShouldContain("@p0");           // SQL text has @
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildWhereClauseEmptyInListUsesAlwaysFalsePredicate()
    {
        // Why: an empty IN list must not emit "col IN ()" (invalid SQL) — the base collapses
        // it to "col IN (SELECT NULL WHERE <AlwaysFalsePredicate>)". For FakeDialect that is
        // FALSE; for T-SQL it would be "1 = 0". This test proves the dialect seam is honoured.
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "team_id",
                Operator = new InOperator(),
                Value = Array.Empty<int>()
            }
        };

        var (sql, captured) = BuildWhere(filter);

        sql.ShouldContain("SELECT NULL WHERE FALSE");
        captured.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildWhereClauseNonEmptyInListExpandsAllItemsAsParams()
    {
        // Why: each IN-list item becomes a numbered param p{cond}_{item} so the SQL stays
        // parameterized. Three items → three addParam calls.
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "region",
                Operator = new InOperator(),
                Value = new[] { 1, 2, 3 }
            }
        };

        var (sql, captured) = BuildWhere(filter);

        captured.Count.ShouldBe(3);
        captured[0].name.ShouldBe("p0_0");
        captured[1].name.ShouldBe("p0_1");
        captured[2].name.ShouldBe("p0_2");
        sql.ShouldContain("@p0_0, @p0_1, @p0_2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildWhereClauseReturnsEmptyStringWhenFilterIsNull()
    {
        // Why: a null filter root must produce an empty string (no WHERE clause),
        // not throw or produce bogus SQL.
        var filter = new FilterExpression { Root = null };

        var (sql, captured) = BuildWhere(filter);

        sql.ShouldBeEmpty();
        captured.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildWhereClauseAndGroupJoinsConditionsWithAnd()
    {
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition { PropertyName = "active", Operator = new EqualOperator(), Value = true },
                    new FilterCondition { PropertyName = "tier", Operator = new EqualOperator(), Value = 2 }
                ]
            }
        };

        var (sql, captured) = BuildWhere(filter);

        sql.ShouldContain("AND");
        sql.ShouldNotContain("OR");
        captured.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildWhereClauseOrGroupJoinsConditionsWithOr()
    {
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.Or,
                Nodes =
                [
                    new FilterCondition { PropertyName = "country", Operator = new EqualOperator(), Value = "US" },
                    new FilterCondition { PropertyName = "country", Operator = new EqualOperator(), Value = "CA" }
                ]
            }
        };

        var (sql, captured) = BuildWhere(filter);

        sql.ShouldContain("OR");
        sql.ShouldNotContain("AND");
        captured.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildWhereClauseCustomParameterPrefixUsedInSqlAndAddParamKey()
    {
        // Why: UPDATE translators namespace WHERE params as "@where_p0" so they don't collide
        // with SET-clause params (@set_name). The prefix appears in the SQL text; the addParam
        // key strips the leading "@" but keeps the namespace ("where_p0").
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "id",
                Operator = new EqualOperator(),
                Value = 99
            }
        };

        var (sql, captured) = BuildWhere(filter, parameterPrefix: "@where_");

        sql.ShouldContain("@where_p0");
        captured.Count.ShouldBe(1);
        captured[0].name.ShouldBe("where_p0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildWhereClauseNullValuePassedToAddParam()
    {
        // Why: SQL NULL must become a DBNull parameter, not be omitted. The base passes
        // null through; the backend-specific AddParameter maps null → DBNull.Value.
        // (This test proves the base doesn't silently discard null values.)
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "deleted_at",
                Operator = new EqualOperator(),
                Value = null
            }
        };

        var (_, captured) = BuildWhere(filter);

        captured.Count.ShouldBe(1);
        captured[0].value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildWhereClauseIsNotNullOperatorProducesNoParam()
    {
        // Why: IS NOT NULL doesn't use a value parameter — it is a unary operator.
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "confirmed_at",
                Operator = new IsNotNullOperator(),
                Value = null
            }
        };

        var (sql, captured) = BuildWhere(filter);

        sql.ShouldContain("\"confirmed_at\" IS NOT NULL");
        captured.ShouldBeEmpty();
    }
}
