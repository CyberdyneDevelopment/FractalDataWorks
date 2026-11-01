using Fdw.Commands.Data.FieldAccess;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests.FieldAccess;

public sealed class PocoFieldExtractorTests
{
    private readonly PocoFieldExtractor _sut = new();

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullForNullRecord()
    {
        _sut.GetValue(null, "Name").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsPropertyValue()
    {
        var entity = new TestEntity { Id = 42, Name = "Acme" };
        _sut.GetValue(entity, "Name").ShouldBe("Acme");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsIntProperty()
    {
        var entity = new TestEntity { Id = 42 };
        _sut.GetValue(entity, "Id").ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullForMissingProperty()
    {
        var entity = new TestEntity();
        _sut.GetValue(entity, "NonExistent").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueIsCaseInsensitive()
    {
        var entity = new TestEntity { Name = "Test" };
        _sut.GetValue(entity, "name").ShouldBe("Test");
        _sut.GetValue(entity, "NAME").ShouldBe("Test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullablePropertyValue()
    {
        var entity = new TestEntity { Price = 19.99m };
        _sut.GetValue(entity, "Price").ShouldBe(19.99m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullForNullNullableProperty()
    {
        var entity = new TestEntity { Price = null };
        _sut.GetValue(entity, "Price").ShouldBeNull();
    }
}
