using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Tests;

public sealed class RowMapperProviderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFactoryReturnsNullWhenNotRegistered()
    {
        var provider = new RowMapperProvider();

        provider.GetFactory("NonExistent").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RegisterFactoryAndGetFactoryReturnsRegistered()
    {
        var provider = new RowMapperProvider();
        var factory = new Mock<IRowMapperFactory>();

        provider.Register("Custom", factory.Object);

        provider.GetFactory("Custom").ShouldBe(factory.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFactoryIsCaseInsensitive()
    {
        var provider = new RowMapperProvider();
        var factory = new Mock<IRowMapperFactory>();

        provider.Register("Pooled", factory.Object);

        provider.GetFactory("pooled").ShouldBe(factory.Object);
        provider.GetFactory("POOLED").ShouldBe(factory.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetDefaultFactoryReturnsRegisteredDefault()
    {
        var provider = new RowMapperProvider();
        var pooledFactory = new Mock<IRowMapperFactory>();

        provider.Register("Pooled", pooledFactory.Object);

        provider.GetDefaultFactory().ShouldBe(pooledFactory.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetDefaultFactoryReturnsFirstWhenDefaultNotRegistered()
    {
        var provider = new RowMapperProvider();
        var customFactory = new Mock<IRowMapperFactory>();

        provider.Register("Custom", customFactory.Object);

        provider.GetDefaultFactory().ShouldBe(customFactory.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetDefaultFactoryReturnsEmptyMapperWhenNoFactories()
    {
        var provider = new RowMapperProvider();

        var factory = provider.GetDefaultFactory();

        factory.ShouldNotBeNull();
        var mapper = factory.Create();
        mapper.ShouldNotBeNull();
        mapper.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SetDefaultTypeChangesDefaultFactory()
    {
        var provider = new RowMapperProvider();
        var pooledFactory = new Mock<IRowMapperFactory>();
        var dynamicFactory = new Mock<IRowMapperFactory>();

        provider.Register("Pooled", pooledFactory.Object);
        provider.Register("Dynamic", dynamicFactory.Object);

        provider.SetDefaultType("Dynamic");

        provider.GetDefaultFactory().ShouldBe(dynamicFactory.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void RegisterFactoryOverwritesExisting()
    {
        var provider = new RowMapperProvider();
        var factory1 = new Mock<IRowMapperFactory>();
        var factory2 = new Mock<IRowMapperFactory>();

        provider.Register("Custom", factory1.Object);
        provider.Register("Custom", factory2.Object);

        provider.GetFactory("Custom").ShouldBe(factory2.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyMapperMapRowReturnsEmptyDictionary()
    {
        var provider = new RowMapperProvider();
        var factory = provider.GetDefaultFactory();
        var mapper = factory.Create();
        var source = new Mock<IRecordCursor>();

        var row = mapper.MapRow(source.Object);

        row.ShouldNotBeNull();
        row.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyMapperEstimatedAllocationsPerRowReturnsOne()
    {
        var provider = new RowMapperProvider();
        var factory = provider.GetDefaultFactory();
        var mapper = factory.Create();

        mapper.EstimatedAllocationsPerRow.ShouldBe(1);
    }
}
