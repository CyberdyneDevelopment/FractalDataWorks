using Fdw.Services.EtlMappers.Dynamic;
using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;

namespace Fdw.Services.EtlMappers.Dynamic.Tests;

public class DynamicStructMapperConfigurationTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapperTypeIsDynamic()
    {
        var sut = new DynamicStructMapperConfiguration();

        sut.MapperType.ShouldBe("Dynamic");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void UseCompiledExpressionsDefaultsToTrue()
    {
        var sut = new DynamicStructMapperConfiguration();

        sut.UseCompiledExpressions.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CacheCompiledDelegatesDefaultsToTrue()
    {
        var sut = new DynamicStructMapperConfiguration();

        sut.CacheCompiledDelegates.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void UseCompiledExpressionsCanBeDisabled()
    {
        var sut = new DynamicStructMapperConfiguration
        {
            UseCompiledExpressions = false
        };

        sut.UseCompiledExpressions.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CacheCompiledDelegatesCanBeDisabled()
    {
        var sut = new DynamicStructMapperConfiguration
        {
            CacheCompiledDelegates = false
        };

        sut.CacheCompiledDelegates.ShouldBeFalse();
    }
}
