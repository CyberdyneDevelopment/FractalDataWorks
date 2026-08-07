using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;
namespace Fdw.Services.EtlMappers.Pooled.Tests;

public sealed class PooledDictionaryMapperConfigurationTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapperTypeReturnsPooled()
    {
        var config = new PooledDictionaryMapperConfiguration();
        config.MapperType.ShouldBe("Pooled");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MaxDictionarySizeDefaultsTo100()
    {
        var config = new PooledDictionaryMapperConfiguration();
        config.MaxDictionarySize.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MaxDictionarySizeCanBeSet()
    {
        var config = new PooledDictionaryMapperConfiguration { MaxDictionarySize = 50 };
        config.MaxDictionarySize.ShouldBe(50);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MaxPoolSizeDefaultsTo1000()
    {
        var config = new PooledDictionaryMapperConfiguration();
        config.MaxPoolSize.ShouldBe(1000);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void EnablePoolingDefaultsToTrue()
    {
        var config = new PooledDictionaryMapperConfiguration();
        config.EnablePooling.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void NameDefaultsToEmptyString()
    {
        var config = new PooledDictionaryMapperConfiguration();
        config.Name.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void NameCanBeSet()
    {
        var config = new PooledDictionaryMapperConfiguration { Name = "MyMapper" };
        config.Name.ShouldBe("MyMapper");
    }
}
