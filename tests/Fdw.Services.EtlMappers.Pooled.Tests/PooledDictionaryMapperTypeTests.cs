using Fdw.Services.EtlMappers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;

namespace Fdw.Services.EtlMappers.Pooled.Tests;

public sealed class PooledDictionaryMapperTypeTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsNameToPooled()
    {
        var sut = new PooledDictionaryMapperType();
        sut.Name.ShouldBe("Pooled");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void EstimatedAllocationsPerRowIsZero()
    {
        var sut = new PooledDictionaryMapperType();
        sut.EstimatedAllocationsPerRow.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void RegisterAddsSingletonFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var sut = new PooledDictionaryMapperType();

        // Act
        sut.Register(services);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(PooledDictionaryMapperFactory));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void RegisterReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var sut = new PooledDictionaryMapperType();

        // Act
        var result = sut.Register(services);

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void RegisterPutsTheFactoryInTheContainerUnderItsFactoryType()
    {
        var services = new ServiceCollection();
        var sut = new PooledDictionaryMapperType();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        sut.Register(services);
        var sp = services.BuildServiceProvider();

        sut.FactoryType.ShouldBe(typeof(PooledDictionaryMapperFactory));
        sp.GetRequiredService(sut.FactoryType).ShouldBeOfType<PooledDictionaryMapperFactory>();
    }
}
