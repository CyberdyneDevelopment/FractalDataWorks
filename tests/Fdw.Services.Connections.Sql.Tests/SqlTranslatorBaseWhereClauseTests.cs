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
