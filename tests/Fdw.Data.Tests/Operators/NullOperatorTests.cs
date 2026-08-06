using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Operators;

public sealed class NullOperatorTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNullHasCorrectProperties()
    {
        var sut = new IsNullOperator();
        sut.Id.ShouldBe(10);
        sut.Name.ShouldBe("IsNull");
        sut.SqlOperator.ShouldBe("IS NULL");
        sut.ODataOperator.ShouldBe("eq null");
        sut.RequiresValue.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNotNullHasCorrectProperties()
    {
        var sut = new IsNotNullOperator();
        sut.Id.ShouldBe(11);
        sut.Name.ShouldBe("IsNotNull");
        sut.SqlOperator.ShouldBe("IS NOT NULL");
        sut.ODataOperator.ShouldBe("ne null");
        sut.RequiresValue.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNullFormatSqlParameterReturnsEmpty()
    {
        new IsNullOperator().FormatSqlParameter("anything").ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNotNullFormatSqlParameterReturnsEmpty()
    {
        new IsNotNullOperator().FormatSqlParameter("anything").ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNullFormatODataValueReturnsEmpty()
    {
        new IsNullOperator().FormatODataValue("ignored").ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNotNullFormatODataValueReturnsEmpty()
    {
        new IsNotNullOperator().FormatODataValue("ignored").ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNullFormatODataValueReturnsEmptyForNull()
    {
        new IsNullOperator().FormatODataValue(null).ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNotNullFormatODataValueReturnsEmptyForNull()
    {
        new IsNotNullOperator().FormatODataValue(null).ShouldBe(string.Empty);
    }
}
