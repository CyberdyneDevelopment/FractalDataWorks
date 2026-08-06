using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class ContainsOperatorTests
{
    private readonly ContainsOperator _sut = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsIdToThree()
    {
        _sut.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsNameToContains()
    {
        _sut.Name.ShouldBe("Contains");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SqlOperatorIsLike()
    {
        _sut.SqlOperator.ShouldBe("LIKE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ODataOperatorIsContains()
    {
        _sut.ODataOperator.ShouldBe("contains");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RequiresValueIsTrue()
    {
        _sut.RequiresValue.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatSqlParameterAddsWildcardsBothSides()
    {
        _sut.FormatSqlParameter("Name").ShouldBe("'%' + @Name + '%'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueWrapsStringInSingleQuotes()
    {
        _sut.FormatODataValue("Corp").ShouldBe("'Corp'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueEscapesSingleQuotes()
    {
        _sut.FormatODataValue("O'Malley").ShouldBe("'O''Malley'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFallsBackToQuotedToStringForNonStrings()
    {
        _sut.FormatODataValue(42).ShouldBe("'42'");
    }
}
