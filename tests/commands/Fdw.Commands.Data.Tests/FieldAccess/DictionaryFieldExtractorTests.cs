using System.Collections.Generic;
using Fdw.Commands.Data.FieldAccess;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests.FieldAccess;

public sealed class DictionaryFieldExtractorTests
{
    private readonly DictionaryFieldExtractor _sut = new();

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
    public void GetValueReturnsDictionaryValue()
    {
        var dict = new Dictionary<string, object> { ["Name"] = "Acme", ["Id"] = 42 };
        _sut.GetValue(dict, "Name").ShouldBe("Acme");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullForMissingKey()
    {
        var dict = new Dictionary<string, object> { ["Name"] = "Acme" };
        _sut.GetValue(dict, "Missing").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsNullForNonDictionaryRecord()
    {
        var entity = new { Name = "Test" };
        _sut.GetValue(entity, "Name").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsIntegerValue()
    {
        var dict = new Dictionary<string, object> { ["Count"] = 99 };
        _sut.GetValue(dict, "Count").ShouldBe(99);
    }
}
