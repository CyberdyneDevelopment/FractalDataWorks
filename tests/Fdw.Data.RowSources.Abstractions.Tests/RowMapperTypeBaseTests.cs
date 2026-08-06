using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Abstractions.Tests;

public class RowMapperTypeBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        var sut = new TestRowMapperType();

        sut.Id.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        var sut = new TestRowMapperType();

        sut.Name.ShouldBe("TestMapper");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EstimatedAllocationsPerRowReturnsConfiguredValue()
    {
        var sut = new TestRowMapperType();

        sut.EstimatedAllocationsPerRow.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsPoolingReturnsConfiguredValue()
    {
        var sut = new TestRowMapperType();

        sut.SupportsPooling.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsDynamicAccessReturnsConfiguredValue()
    {
        var sut = new TestRowMapperType();

        sut.SupportsDynamicAccess.ShouldBeFalse();
    }

    private sealed class TestRowMapperType : RowMapperTypeBase
    {
        public TestRowMapperType() : base(5, "TestMapper") { }

        public override int EstimatedAllocationsPerRow => 3;
        public override bool SupportsPooling => true;
        public override bool SupportsDynamicAccess => false;
    }
}
