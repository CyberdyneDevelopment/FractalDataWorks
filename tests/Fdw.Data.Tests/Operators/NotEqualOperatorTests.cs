using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class NotEqualOperatorTests
{
    private readonly NotEqualOperator _sut = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsIdToTwo()
    {
        _sut.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsNameToNotEqual()
    {
        _sut.Name.ShouldBe("NotEqual");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SqlOperatorIsNotEqualsSign()
    {
        _sut.SqlOperator.ShouldBe("<>");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ODataOperatorIsNe()
    {
        _sut.ODataOperator.ShouldBe("ne");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RequiresValueIsTrue()
    {
        _sut.RequiresValue.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueReturnsNullStringForNull()
    {
        _sut.FormatODataValue(null).ShouldBe("null");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueWrapsStringInSingleQuotes()
    {
        _sut.FormatODataValue("test").ShouldBe("'test'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueEscapesSingleQuotesInStrings()
    {
        _sut.FormatODataValue("it's").ShouldBe("'it''s'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsIntWithoutQuotes()
    {
        _sut.FormatODataValue(100).ShouldBe("100");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsBoolAsLowercase()
    {
        _sut.FormatODataValue(true).ShouldBe("true");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsGuidWithPrefix()
    {
        var guid = Guid.NewGuid();
        _sut.FormatODataValue(guid).ShouldStartWith("guid'");
    }
}
