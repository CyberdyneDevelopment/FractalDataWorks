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
    public void MaxDictionarySizeIsUnstatedUntilSet()
    {
        // FDW-3956: the Pooled type declares it; the configuration carries no value of its own.
        var config = new PooledDictionaryMapperConfiguration();
        config.MaxDictionarySize.ShouldBeNull();
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
    public void MaxPoolSizeIsUnstatedUntilSet()
    {
        var config = new PooledDictionaryMapperConfiguration();
        config.MaxPoolSize.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void EnablePoolingIsUnstatedUntilSet()
    {
        var config = new PooledDictionaryMapperConfiguration();
        config.EnablePooling.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ThePooledTypeDeclaresWhatAConfigurationLeavesOut()
    {
        var declared = new PooledDictionaryMapperType().DeclaredDefaults;
        declared[nameof(PooledDictionaryMapperConfiguration.MaxPoolSize)].ShouldBe(1000);
        declared[nameof(PooledDictionaryMapperConfiguration.MaxDictionarySize)].ShouldBe(100);
        declared[nameof(PooledDictionaryMapperConfiguration.EnablePooling)].ShouldBe(true);
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
