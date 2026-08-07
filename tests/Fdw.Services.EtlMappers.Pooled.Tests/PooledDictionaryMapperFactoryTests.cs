using Fdw.Services.EtlMappers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;

namespace Fdw.Services.EtlMappers.Pooled.Tests;

public sealed class PooledDictionaryMapperFactoryTests
{
    private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateWithPooledConfigurationReturnsSuccess()
    {
        // Arrange
        var factory = new PooledDictionaryMapperFactory(_loggerFactory);
        var config = new PooledDictionaryMapperConfiguration
        {
            Name = "TestMapper",
            MaxPoolSize = 500,
            MaxDictionarySize = 50
        };

        // Act
        var result = factory.Create(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeOfType<PooledDictionaryMapper>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateWithDefaultConfigurationReturnsSuccess()
    {
        // Arrange
        var factory = new PooledDictionaryMapperFactory(_loggerFactory);
        var config = new PooledDictionaryMapperConfiguration();

        // Act
        var result = factory.Create(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateViaInterfaceWithPooledConfigurationReturnsSuccess()
    {
        // Arrange
        IEtlRowMapperFactory factory = new PooledDictionaryMapperFactory(_loggerFactory);
        EtlRowMapperConfiguration config = new PooledDictionaryMapperConfiguration
        {
            Name = "TestMapper"
        };

        // Act
        var result = factory.Create(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateViaInterfaceWithNonPooledConfigurationCreatesWithDefaults()
    {
        // Arrange
        IEtlRowMapperFactory factory = new PooledDictionaryMapperFactory(_loggerFactory);
        var mockConfig = new TestEtlRowMapperConfig { Name = "TestMapper", EnablePooling = true, MaxPoolSize = 500 };
        // Act
        var result = factory.Create(mockConfig);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateReturnedMapperHasZeroAllocationsPerRow()
    {
        // Arrange
        var factory = new PooledDictionaryMapperFactory(_loggerFactory);
        var config = new PooledDictionaryMapperConfiguration();

        // Act
        var result = factory.Create(config);

        // Assert
        result.Value!.EstimatedAllocationsPerRow.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateReturnsFailureWhenConstructorThrows()
    {
        // Arrange - make the logger factory throw when creating a logger for PooledDictionaryMapper
        var throwingLoggerFactory = new Mock<ILoggerFactory>();
        throwingLoggerFactory
            .Setup(f => f.CreateLogger(It.Is<string>(s => s.Contains("PooledDictionaryMapperFactory"))))
            .Returns(new Mock<ILogger>().Object);
        throwingLoggerFactory
            .Setup(f => f.CreateLogger(It.Is<string>(s => s.Contains("PooledDictionaryMapper") && !s.Contains("Factory"))))
            .Throws(new InvalidOperationException("Logger creation failed"));

        var factory = new PooledDictionaryMapperFactory(throwingLoggerFactory.Object);
        var config = new PooledDictionaryMapperConfiguration { Name = "test-mapper" };

        // Act
        var result = factory.Create(config);

        // Assert - should hit the catch block
        result.IsSuccess.ShouldBeFalse();
    }


    private sealed class TestEtlRowMapperConfig : EtlRowMapperConfiguration
    {
        public TestEtlRowMapperConfig() : base("EtlMapper", null, "EtlMappers") { }
    }
}
