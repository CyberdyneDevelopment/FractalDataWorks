using System;
using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Sql.Tests.Fakes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// FDW-996: Contains, StartsWith and EndsWith are LIKE patterns. The operator states where anything may sit
/// around its value, the dialect states what its LIKE reads as syntax, and the translator joins the two:
/// the parameter's VALUE is the escaped text with the wildcards, and the statement carries an ESCAPE clause.
/// Before this, the wildcards were never applied and all three matched exactly.
/// </summary>
[Collection(nameof(SqlTranslatorTestCollection))]
public sealed class SqlTranslatorBasePatternOperatorTests
{
    private static (string sql, List<(string name, object? value)> captured) BuildWhere(
        FilterOperatorBase op, object? value, FakeDialect? dialect = null, string parameterPrefix = "@")
    {
        var captured = new List<(string, object?)>();
        var sql = SqlTranslatorProxy.ExposeWhereClause(
            new FilterExpression
            {
                Root = new FilterCondition { PropertyName = "name", Operator = op, Value = value },
            },
            dialect ?? new FakeDialect(),
            (name, parameter) => captured.Add((name, parameter)),
            parameterPrefix: parameterPrefix);
        return (sql, captured);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ContainsPutsAWildcardOnBothSidesOfTheValue()
    {
        var (sql, captured) = BuildWhere(new ContainsOperator(), "afc");

        sql.ShouldBe("\"name\" LIKE @p0 ESCAPE '!'");
        captured.ShouldHaveSingleItem().ShouldBe(("p0", (object?)"%afc%"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void StartsWithPutsAWildcardAfterTheValueOnly()
    {
        var (sql, captured) = BuildWhere(new StartsWithOperator(), "afc");

        sql.ShouldBe("\"name\" LIKE @p0 ESCAPE '!'");
        captured.ShouldHaveSingleItem().ShouldBe(("p0", (object?)"afc%"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void EndsWithPutsAWildcardBeforeTheValueOnly()
    {
        var (sql, captured) = BuildWhere(new EndsWithOperator(), "afc");

        sql.ShouldBe("\"name\" LIKE @p0 ESCAPE '!'");
        captured.ShouldHaveSingleItem().ShouldBe(("p0", (object?)"%afc"));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    [InlineData("a_b", "%a!_b%")]
    [InlineData("50%", "%50!%%")]
    [InlineData("[x]", "%![x]%")]
    [InlineData("a!b", "%a!!b%")]
    [InlineData("!%_[", "%!!!%!_![%")]
    public void ContainsEscapesEveryMetacharacterAndTheEscapeCharacterItself(string text, string expected)
    {
        var (_, captured) = BuildWhere(new ContainsOperator(), text);

        captured.ShouldHaveSingleItem().value.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void OnlyTheCharactersTheDialectNamesAreEscaped()
    {
        // A dialect whose LIKE has no character sets leaves a bracket alone: what needs escaping is the
        // dialect's fact, not the operator's, which is why the fix does not live in the operator.
        var postgres = new FakeDialect(likeMetaCharacters: "%_");

        var (_, captured) = BuildWhere(new ContainsOperator(), "[x]_", postgres);

        captured.ShouldHaveSingleItem().value.ShouldBe("%[x]!_%");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void TheEscapeClauseNamesTheDialectsEscapeCharacter()
    {
        var dialect = new FakeDialect(likeEscapeCharacter: '~');

        var (sql, captured) = BuildWhere(new StartsWithOperator(), "a~b", dialect);

        sql.ShouldBe("\"name\" LIKE @p0 ESCAPE '~'");
        captured.ShouldHaveSingleItem().value.ShouldBe("a~~b%");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void TheOperatorWrittenIsTheOneTheDialectSaysIgnoresCase()
    {
        var caseSensitiveLike = new FakeDialect(patternMatchOperator: "ILIKE");

        var (sql, _) = BuildWhere(new ContainsOperator(), "afc", caseSensitiveLike);

        sql.ShouldBe("\"name\" ILIKE @p0 ESCAPE '!'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void TheParameterPrefixComesFromTheContextNotFromTheOperator()
    {
        var (sql, captured) = BuildWhere(new ContainsOperator(), "x", parameterPrefix: ":");

        sql.ShouldBe("\"name\" LIKE :p0 ESCAPE '!'");
        captured.ShouldHaveSingleItem().name.ShouldBe("p0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ANumberIsMatchedAsItsText()
    {
        var (_, captured) = BuildWhere(new ContainsOperator(), 25);

        captured.ShouldHaveSingleItem().value.ShouldBe("%25%");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ANullValueStaysNullSoNothingMatches()
    {
        var (sql, captured) = BuildWhere(new ContainsOperator(), null);

        sql.ShouldBe("\"name\" LIKE @p0 ESCAPE '!'");
        captured.ShouldHaveSingleItem().value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AListIsRefusedRatherThanJoinedIntoAPattern()
        => Should.Throw<ArgumentException>(() => BuildWhere(new StartsWithOperator(), new[] { "a", "b" }));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ADialectThatNamesTheSingleQuoteAsItsEscapeIsRefused()
        => Should.Throw<InvalidOperationException>(
            () => BuildWhere(new ContainsOperator(), "x", new FakeDialect(likeEscapeCharacter: '\'')));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ComparisonOperatorsAreStillWrittenWithoutAnEscapeClause()
    {
        var (sql, captured) = BuildWhere(new EqualOperator(), "afc");

        sql.ShouldBe("\"name\" = @p0");
        captured.ShouldHaveSingleItem().value.ShouldBe("afc");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void EachPatternOperatorStatesItsShapeOnce()
    {
        var contains = new ContainsOperator();
        var startsWith = new StartsWithOperator();
        var endsWith = new EndsWithOperator();

        (contains.IsPatternMatch, contains.MatchesAnythingBefore, contains.MatchesAnythingAfter).ShouldBe((true, true, true));
        (startsWith.IsPatternMatch, startsWith.MatchesAnythingBefore, startsWith.MatchesAnythingAfter).ShouldBe((true, false, true));
        (endsWith.IsPatternMatch, endsWith.MatchesAnythingBefore, endsWith.MatchesAnythingAfter).ShouldBe((true, true, false));
        new EqualOperator().IsPatternMatch.ShouldBeFalse();
        new InOperator().IsPatternMatch.ShouldBeFalse();
        new IsNullOperator().IsPatternMatch.ShouldBeFalse();
    }
}
