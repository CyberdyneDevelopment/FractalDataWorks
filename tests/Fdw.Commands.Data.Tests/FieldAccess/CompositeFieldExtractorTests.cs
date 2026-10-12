using System.Collections.Generic;
using Fdw.Commands.Data.FieldAccess;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests.FieldAccess;

public sealed class CompositeFieldExtractorTests
{
    private readonly CompositeFieldExtractor _sut = new();

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
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
    public void GetValueExtractsFromDictionary()
    {
        var dict = new Dictionary<string, object> { ["Name"] = "FromDict" };
        _sut.GetValue(dict, "Name").ShouldBe("FromDict");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueExtractsFromPoco()
    {
        var entity = new TestEntity { Id = 42, Name = "FromPoco" };
        _sut.GetValue(entity, "Name").ShouldBe("FromPoco");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullForMissingFieldOnPoco()
    {
        var entity = new TestEntity();
        _sut.GetValue(entity, "NonExistent").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullForMissingKeyOnDictionary()
    {
        var dict = new Dictionary<string, object> { ["Name"] = "Test" };
        _sut.GetValue(dict, "Missing").ShouldBeNull();
    }
}
