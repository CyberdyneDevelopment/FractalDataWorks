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
    public void UseCompiledExpressionsIsUnstatedUntilSet()
    {
        // FDW-3956: the Dynamic type declares it; the configuration carries no value of its own.
        var sut = new DynamicStructMapperConfiguration();

        sut.UseCompiledExpressions.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CacheCompiledDelegatesIsUnstatedUntilSet()
    {
        var sut = new DynamicStructMapperConfiguration();

        sut.CacheCompiledDelegates.ShouldBeNull();
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

        sut.UseCompiledExpressions.ShouldBe(false);
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

        sut.CacheCompiledDelegates.ShouldBe(false);
    }
}
