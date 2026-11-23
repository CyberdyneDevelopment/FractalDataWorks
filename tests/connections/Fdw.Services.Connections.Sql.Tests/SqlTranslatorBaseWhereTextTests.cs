using System;
using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Sql.Tests.Fakes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// The WHERE text and its parameters, asserted byte for byte for each filter node kind, a group, a group inside a group and an empty
/// group (FDW-4403). The filter nodes are written by the handler named for their kind, and the building moved out of the generic translator
/// base; the expected texts are what the code BEFORE that move wrote (these assertions pass unchanged against the parent of the move,
/// a33d014b1b), so a handler that wrote anything else would fail here. The FakeDialect quotes as <c>"name"</c>.
/// </summary>
[Collection(nameof(SqlTranslatorTestCollection))]
public sealed class SqlTranslatorBaseWhereTextTests
{
    private static readonly FakeDialect Dialect = new();

    private static (string Sql, List<(string Name, object? Value)> Parameters) Build(
        IFilterNode root,
        string? primaryTableQualifier = null,
        string parameterPrefix = "@")
    {
        var captured = new List<(string, object?)>();
        var sql = SqlTranslatorProxy.ExposeWhereClause(
            new FilterExpression { Root = root },
            Dialect,
            (name, value) => captured.Add((name, value)),
            primaryTableQualifier,
            parameterPrefix);
        return (sql, captured);
    }

    private static FilterCondition Equal(string property, object value) =>
        new() { PropertyName = property, Operator = new EqualOperator(), Value = value };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ACondition()
    {
        var (sql, parameters) = Build(Equal("status", "active"));

        sql.ShouldBe("\"status\" = @p0");
        parameters.ShouldBe([("p0", (object?)"active")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AConditionThatNeedsNoValue()
    {
        var (sql, parameters) = Build(new FilterCondition { PropertyName = "deleted", Operator = new IsNullOperator() });

        sql.ShouldBe("\"deleted\" IS NULL");
        parameters.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AListConditionIsOneParameterPerItem()
    {
        // A key list is built inside the connection's scope; this list is within its setting (as the sibling WhereClause tests do).
        using var scope = KeyListSpillScope.Begin(new TranslationSettings(50), "[t]");

        var (sql, parameters) = Build(new FilterCondition { PropertyName = "id", Operator = new InOperator(), Value = new[] { 1, 2, 3 } });

        sql.ShouldBe("\"id\" IN (@p0_0, @p0_1, @p0_2)");
        parameters.ShouldBe([("p0_0", (object?)1), ("p0_1", 2), ("p0_2", 3)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AnAndGroupIsWrappedInParentheses()
    {
        var (sql, parameters) = Build(new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = [Equal("active", true), Equal("tier", 2)],
        });

        sql.ShouldBe("(\"active\" = @p0 AND \"tier\" = @p1)");
        parameters.ShouldBe([("p0", (object?)true), ("p1", 2)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AnOrGroupIsWrappedInParentheses()
    {
        var (sql, _) = Build(new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes = [Equal("country", "US"), Equal("country", "CA")],
        });

        sql.ShouldBe("(\"country\" = @p0 OR \"country\" = @p1)");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AGroupOfOneConditionIsThatConditionWithoutParentheses()
    {
        var (sql, _) = Build(new FilterGroup { Operator = LogicalOperator.And, Nodes = [Equal("active", true)] });

        sql.ShouldBe("\"active\" = @p0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AnEmptyGroupWritesNothing()
    {
        var (sql, parameters) = Build(new FilterGroup { Operator = LogicalOperator.And, Nodes = [] });

        sql.ShouldBe(string.Empty);
        parameters.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AGroupInsideAGroupWithAConditionInEach()
    {
        var (sql, parameters) = Build(new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes =
            [
                new FilterGroup { Operator = LogicalOperator.And, Nodes = [Equal("a", 1), Equal("b", 2)] },
                Equal("c", 3),
            ],
        });

        sql.ShouldBe("((\"a\" = @p0 AND \"b\" = @p1) OR \"c\" = @p2)");
        parameters.ShouldBe([("p0", (object?)1), ("p1", 2), ("p2", 3)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AnEmptyGroupInsideAGroupIsLeftOut()
    {
        var (sql, _) = Build(new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = [Equal("a", 1), new FilterGroup { Operator = LogicalOperator.Or, Nodes = [] }],
        });

        sql.ShouldBe("\"a\" = @p0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AColumnIsQualifiedByItsTableAndTheParameterPrefixIsKept()
    {
        var (bare, _) = Build(Equal("id", 1), primaryTableQualifier: "Child", parameterPrefix: "@where_");
        var (dotted, _) = Build(Equal("Parent.Name", "x"), primaryTableQualifier: "Child");

        bare.ShouldBe("\"Child\".\"id\" = @where_p0");
        dotted.ShouldBe("\"Parent\".\"Name\" = @p0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ANodeThatIsNeitherAConditionNorAGroupIsRefused()
    {
        Should.Throw<InvalidOperationException>(() => Build(new UnknownNode()))
            .Message.ShouldBe($"Unknown filter node type: {nameof(UnknownNode)}");
    }

    private sealed class UnknownNode : IFilterNode
    {
    }
}
