using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers.Dynamic;
using Microsoft.Extensions.Logging;
using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;

namespace Fdw.Services.EtlMappers.Dynamic.Tests;

public class DynamicStructMapperFactoryTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory = new();

    public DynamicStructMapperFactoryTests()
    {
        _mockLoggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateReturnsSuccessWithValidConfiguration()
    {
        var sut = new DynamicStructMapperFactory(_mockLoggerFactory.Object);
        var config = new DynamicStructMapperConfiguration { Name = "test-mapper" };

        var result = sut.Create(config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateReturnsMapperInstanceOfCorrectType()
    {
        var sut = new DynamicStructMapperFactory(_mockLoggerFactory.Object);
        var config = new DynamicStructMapperConfiguration();

        var result = sut.Create(config);

        result.Value.ShouldBeOfType<DynamicStructMapper>();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void NonGenericCreateWithDynamicConfigReturnsSuccess()
    {
        IEtlRowMapperFactory sut = new DynamicStructMapperFactory(_mockLoggerFactory.Object);
        var config = new DynamicStructMapperConfiguration { Name = "test-mapper" };

        var result = sut.Create(config);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void NonGenericCreateWithNonDynamicConfigUsesDefaults()
    {
        IEtlRowMapperFactory sut = new DynamicStructMapperFactory(_mockLoggerFactory.Object);
        var mockConfig = new TestEtlRowMapperConfig { Name = "other", EnablePooling = false, MaxPoolSize = 100 };
        var result = sut.Create(mockConfig);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreatedMapperIsNotInitialized()
    {
        var sut = new DynamicStructMapperFactory(_mockLoggerFactory.Object);
        var config = new DynamicStructMapperConfiguration();

        var result = sut.Create(config);

        result.Value!.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void CreateReturnsFailureWhenConstructorThrows()
    {
        // Arrange - make the logger factory throw when creating a logger for DynamicStructMapper
        var throwingLoggerFactory = new Mock<ILoggerFactory>();
        throwingLoggerFactory
            .Setup(f => f.CreateLogger(It.Is<string>(s => s.Contains("DynamicStructMapperFactory"))))
            .Returns(new Mock<ILogger>().Object);
        throwingLoggerFactory
            .Setup(f => f.CreateLogger(It.Is<string>(s => s.Contains("DynamicStructMapper") && !s.Contains("Factory"))))
            .Throws(new InvalidOperationException("Logger creation failed"));

        var sut = new DynamicStructMapperFactory(throwingLoggerFactory.Object);
        var config = new DynamicStructMapperConfiguration { Name = "test-mapper" };

        // Act
        var result = sut.Create(config);

        // Assert - should hit the catch block
        result.IsSuccess.ShouldBeFalse();
    }


    private sealed class TestEtlRowMapperConfig : EtlRowMapperConfiguration
    {
        public TestEtlRowMapperConfig() : base("EtlMapper", null, "EtlMappers") { }
    }
}
