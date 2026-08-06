using Fdw.Services.EtlMappers;
using Fdw.Services.EtlMappers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.EtlMappers.Tests;

/// <summary>
/// Tests for the EtlRowMapperProvider.
/// </summary>
public class EtlRowMapperProviderTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly EtlRowMapperProvider _provider;

    public EtlRowMapperProviderTests()
    {
        _loggerFactory = LoggerFactory.Create(_ => { });
        _provider = new EtlRowMapperProvider(_loggerFactory.CreateLogger<EtlRowMapperProvider>());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultMapperTypeIsPooled()
    {
        // Assert
        _provider.DefaultMapperType.ShouldBe("Pooled");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RegisterAddsFactoryToProvider()
    {
        // Arrange
        var factory = new Mock<IEtlRowMapperFactory>();

        // Act
        _provider.Register("TestMapper", factory.Object);

        // Assert
        _provider.MapperTypeCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RegisterThrowsOnNullOrEmptyMapperType()
    {
        // Arrange
        var factory = new Mock<IEtlRowMapperFactory>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _provider.Register(null!, factory.Object));
        Should.Throw<ArgumentNullException>(() => _provider.Register("", factory.Object));
        Should.Throw<ArgumentNullException>(() => _provider.Register("  ", factory.Object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RegisterThrowsOnNullFactory()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _provider.Register("Test", null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CreateReturnsFailureWhenConfigurationIsNull()
    {
        // Act
        var result = _provider.Create(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CreateReturnsFailureForUnknownMapperType()
    {
        // Arrange
        var config = new TestEtlRowMapperConfig { ServiceOptionType = "Unknown" };
        // Act
        var result = _provider.Create(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CreateUsesDefaultMapperTypeWhenNotSpecified()
    {
        // Arrange
        var config = new TestEtlRowMapperConfig { ServiceOptionType = string.Empty };
        // Act - will fail because no factory registered but error message shows it tried the default
        var result = _provider.Create(config);

        // Assert - it should try to use "Pooled" (the default)
        result.IsSuccess.ShouldBeFalse();
        result.Messages.First().Message.ShouldContain("Pooled");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CreateCallsRegisteredFactory()
    {
        // Arrange
        var mapper = new Mock<IEtlRowMapper>();
        var factory = new Mock<IEtlRowMapperFactory>();
        factory.Setup(f => f.Create(It.IsAny<EtlRowMapperConfiguration>()))
            .Returns(Fdw.Results.GenericResult<IEtlRowMapper>.Success(mapper.Object));

        var config = new TestEtlRowMapperConfig { ServiceOptionType = "TestMapper" };
        _provider.Register("TestMapper", factory.Object);

        // Act
        var result = _provider.Create(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(mapper.Object);
        factory.Verify(f => f.Create(config), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RegisterIsCaseInsensitive()
    {
        // Arrange
        var factory = new Mock<IEtlRowMapperFactory>();
        var mapper = new Mock<IEtlRowMapper>();
        factory.Setup(f => f.Create(It.IsAny<EtlRowMapperConfiguration>()))
            .Returns(Fdw.Results.GenericResult<IEtlRowMapper>.Success(mapper.Object));

        var config = new TestEtlRowMapperConfig { ServiceOptionType = "TESTMAPPER" };
        _provider.Register("TestMapper", factory.Object);

        // Act
        var result = _provider.Create(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CompleteInitializationDoesNotThrow()
    {
        // Arrange
        var factory = new Mock<IEtlRowMapperFactory>();
        _provider.Register("TestMapper", factory.Object);

        // Act - should log the count and not throw
        _provider.CompleteInitialization();

        // Assert
        _provider.MapperTypeCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CompleteInitializationWithNoFactoriesDoesNotThrow()
    {
        // Act - should log zero count and not throw
        _provider.CompleteInitialization();

        // Assert
        _provider.MapperTypeCount.ShouldBe(0);
    }


    private sealed class TestEtlRowMapperConfig : EtlRowMapperConfiguration
    {
        public TestEtlRowMapperConfig() : base("EtlMapper", null, "EtlMappers") { }
    }
}
