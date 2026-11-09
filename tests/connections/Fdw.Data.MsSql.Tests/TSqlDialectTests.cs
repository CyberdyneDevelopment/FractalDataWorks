using System;
using Fdw.Data.MsSql;
using Fdw.Services.Connections.Sql;

namespace Fdw.Data.MsSql.Tests;

/// <summary>
/// FDW-996: what T-SQL's LIKE reads as pattern syntax, and the escape character its statements name, are
/// the dialect's facts. The shared translator builds every pattern from them.
/// </summary>
public sealed class TSqlDialectTests
{
    private readonly TSqlDialect _sut = TSqlDialect.Instance;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void AKeyListGoesToATemporaryTableTypedLikeTheFilteredColumn()
    {
        var form = ((IKeyListSpillDialect)_sut).KeyListSpill("abc123", "[dbo].[Orders]", "Id", "@kl0");

        form.Before.ShouldContain("SELECT TOP 0 ISNULL([Id], [Id]) AS [Value] INTO #fdw_k_abc123 FROM [dbo].[Orders]");
        form.Before.ShouldContain("OPENJSON(@kl0)");
        form.Subquery.ShouldBe("SELECT [Value] FROM #fdw_k_abc123");
        form.After.ShouldContain("DROP TABLE #fdw_k_abc123");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void AKeyListTravelsAsAJsonArrayOfScalars()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        ((IKeyListSpillDialect)_sut).KeyListJson("Id", [1, "a", id]).ShouldBe("[1,\"a\",\"11111111-2222-3333-4444-555555555555\"]");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void TheLikeEscapeCharacterIsAnExclamationMark()
    {
        _sut.LikeEscapeCharacter.ShouldBe('!');
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void TSqlReadsPercentUnderscoreAndOpeningBracketAsPatternSyntax()
    {
        _sut.LikeMetaCharacters.ShouldBe("%_[");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void TheEscapeCharacterIsNotAlsoAMetacharacterAndIsNotASingleQuote()
    {
        _sut.LikeMetaCharacters.ShouldNotContain(_sut.LikeEscapeCharacter);
        _sut.LikeEscapeCharacter.ShouldNotBe('\'');
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void TSqlMatchesPatternsWithLikeWhichIgnoresCaseUnderItsDefaultCollation()
    {
        _sut.PatternMatchOperator.ShouldBe("LIKE");
    }
}
