using Fdw.Data.MsSql;

namespace Fdw.Data.MsSql.Tests;

/// <summary>
/// FDW-996: what T-SQL's LIKE reads as pattern syntax, and the escape character its statements name, are
/// the dialect's facts. The shared translator builds every pattern from them.
/// </summary>
public sealed class TSqlDialectTests
{
    private readonly TSqlDialect _sut = TSqlDialect.Instance;

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
