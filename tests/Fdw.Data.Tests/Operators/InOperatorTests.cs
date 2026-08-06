using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class InOperatorTests
{
    private readonly InOperator _sut = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsIdToTwelve()
    {
        _sut.Id.ShouldBe(12);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsNameToIn()
    {
        _sut.Name.ShouldBe("In");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SqlOperatorIsIN()
    {
        _sut.SqlOperator.ShouldBe("IN");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ODataOperatorIsIn()
    {
        _sut.ODataOperator.ShouldBe("in");
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
    public void FormatSqlParameterAddsAtPrefix()
    {
        _sut.FormatSqlParameter("Status").ShouldBe("@Status");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueReturnsEmptyParensForNull()
    {
        _sut.FormatODataValue(null).ShouldBe("()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsStringEnumerable()
    {
        var values = new[] { "Active", "Pending" };
        _sut.FormatODataValue(values).ShouldBe("('Active','Pending')");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsIntEnumerable()
    {
        var values = new[] { 1, 2, 3 };
        _sut.FormatODataValue(values).ShouldBe("(1,2,3)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueEscapesSingleQuotesInStringCollection()
    {
        var values = new[] { "O'Brien", "Normal" };
        _sut.FormatODataValue(values).ShouldBe("('O''Brien','Normal')");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsDecimalEnumerable()
    {
        var values = new[] { 1.5m, 2.5m };
        _sut.FormatODataValue(values).ShouldBe("(1.5,2.5)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsMixedTypeEnumerable()
    {
        var values = new List<object> { "text", 42 };
        var result = _sut.FormatODataValue(values);
        result.ShouldBe("('text',42)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueWrapsNonEnumerableInParens()
    {
        _sut.FormatODataValue("single").ShouldBe("('single')");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatODataValueFormatsEmptyCollection()
    {
        var values = Array.Empty<string>();
        _sut.FormatODataValue(values).ShouldBe("()");
    }
}
