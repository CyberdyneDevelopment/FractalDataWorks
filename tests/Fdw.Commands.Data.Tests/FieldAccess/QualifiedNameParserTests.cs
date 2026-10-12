using Fdw.Commands.Data.FieldAccess;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests.FieldAccess;

public sealed class QualifiedNameParserTests
{
    private readonly QualifiedNameParser _sut = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsFieldNameFromQualifiedName()
    {
        _sut.GetFieldName("Customers.Id").ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsUnqualifiedNameAsIs()
    {
        _sut.GetFieldName("Id").ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameHandlesMultipleDots()
    {
        _sut.GetFieldName("Schema.Table.Column").ShouldBe("Column");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsEmptyStringForEmptyInput()
    {
        _sut.GetFieldName("").ShouldBe("");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsNullForNullInput()
    {
        _sut.GetFieldName(null!).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameHandlesTrailingDot()
    {
        _sut.GetFieldName("Customers.").ShouldBe("");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameHandlesLeadingDot()
    {
        _sut.GetFieldName(".Field").ShouldBe("Field");
    }
}
