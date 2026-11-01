using Fdw.Services.EtlMappers.Dynamic;
using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;

namespace Fdw.Services.EtlMappers.Dynamic.Tests;

public class DynamicStructMapperTypeTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void NameIsDynamic()
    {
        var sut = new DynamicStructMapperType();

        sut.Name.ShouldBe("Dynamic");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void EstimatedAllocationsPerRowReturnsOne()
    {
        var sut = new DynamicStructMapperType();

        sut.EstimatedAllocationsPerRow.ShouldBe(1);
    }
}
