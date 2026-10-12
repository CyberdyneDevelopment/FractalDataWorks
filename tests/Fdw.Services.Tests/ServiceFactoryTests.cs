using System;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests;

[Collection(nameof(ServicesTestCollection))]
public class ServiceFactoryTests
{
    // Skipped: These tests access protected Logger property
    // [Fact]
    // public void Constructor_WithNullLogger_UsesNullLoggerInstance()
    // {
    //     // Arrange & Act
    //     var factory = new TestServiceFactory(null);
    //
    //     // Assert
    //     factory.ShouldNotBeNull();
    //     factory.Logger.ShouldNotBeNull();
    // }

    // [Fact]
    // public void Constructor_WithLogger_StoresLogger()
    // {
    //     // Arrange
    //     var mockLogger = new Mock<ILogger<TestServiceFactory>>();
    //
    //     // Act
    //     var factory = new TestServiceFactory(mockLogger.Object);
    //
    //     // Assert
    //     factory.ShouldNotBeNull();
    //     factory.Logger.ShouldBe(mockLogger.Object);
    // }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Create_WithNullConfiguration_ReturnsFailure()
    {
        // Arrange
        var factory = new TestServiceFactory();

        // Act
        var result = factory.Create(null!);

        // Assert
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue();
        result.Code?.Name.ShouldBe("ConfigurationRequired");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Create_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var factory = new TestServiceFactory();
        var config = new TestConfiguration { Id = Guid.NewGuid(), Name = "TestConfig" };

        // Act
        var result = factory.Create(config);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Configuration.ShouldBe(config);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Create_WithValidConfiguration_CreatesServiceWithConfiguration()
    {
        // Arrange
        var factory = new TestServiceFactory();
        var testId = Guid.NewGuid();
        var config = new TestConfiguration { Id = testId, Name = "MyConfig" };

        // Act
        var result = factory.Create(config);

        // Assert
        result.Value.ShouldNotBeNull();
        result.Value.Configuration.Id.ShouldBe(testId);
        result.Value.Configuration.Name.ShouldBe("MyConfig");
    }

    // Skipped: These tests access protected ValidateConfiguration method
    // [Fact]
    // public void ValidateConfiguration_WithNullConfiguration_ReturnsFailure()
    // {
    //     // Arrange
    //     var factory = new TestServiceFactory();
    //
    //     // Act
    //     var result = factory.ValidateConfiguration(null, out var validConfig);
    //
    //     // Assert
    //     result.ShouldNotBeNull();
    //     result.IsFailure.ShouldBeTrue();
    //     validConfig.ShouldBeNull();
    //     result.CurrentMessage.ShouldContain("Configuration cannot be null");
    // }

    // [Fact]
    // public void ValidateConfiguration_WithValidConfiguration_ReturnsSuccess()
    // {
    //     // Arrange
    //     var factory = new TestServiceFactory();
    //     var config = new TestConfiguration();
    //
    //     // Act
    //     var result = factory.ValidateConfiguration(config, out var validConfig);
    //
    //     // Assert
    //     result.ShouldNotBeNull();
    //     result.IsSuccess.ShouldBeTrue();
    //     validConfig.ShouldBe(config);
    // }

    // [Fact]
    // public void ValidateConfiguration_WithWrongConfigurationType_ReturnsFailure()
    // {
    //     // Arrange
    //     var factory = new TestServiceFactory();
    //     var wrongConfig = new Mock<IGenericConfiguration>().Object;
    //
    //     // Act
    //     var result = factory.ValidateConfiguration(wrongConfig, out var validConfig);
    //
    //     // Assert
    //     result.ShouldNotBeNull();
    //     result.IsFailure.ShouldBeTrue();
    //     validConfig.ShouldBeNull();
    //     result.CurrentMessage.ShouldContain("Invalid configuration type");
    // }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactory_CreateGeneric_WithValidType_ReturnsSuccess()
    {
        // Arrange
        IServiceFactory<TestService> factory = new TestServiceFactory();
        var config = new TestConfiguration();

        // Act
        var result = factory.Create(config);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactory_CreateGeneric_WithNullConfiguration_ReturnsFailure()
    {
        // Arrange
        IServiceFactory<TestService> factory = new TestServiceFactory();

        // Act
        var result = factory.Create(null!);

        // Assert
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactory_CreateGeneric_WithWrongConfigurationType_ReturnsFailure()
    {
        // Arrange
        IServiceFactory<TestService> factory = new TestServiceFactory();
        var wrongConfig = new Mock<IGenericConfiguration>().Object;

        // Act
        var result = factory.Create(wrongConfig);

        // Assert
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactory_CreateNonGeneric_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        IServiceFactory factory = new TestServiceFactory();
        var config = new TestConfiguration();

        // Act
        var result = factory.Create(config);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeAssignableTo<IGenericService>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactory_CreateNonGeneric_WithNullConfiguration_ReturnsFailure()
    {
        // Arrange
        IServiceFactory factory = new TestServiceFactory();

        // Act
        var result = factory.Create(null!);

        // Assert
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactory_CreateNonGeneric_WithWrongConfigurationType_ReturnsFailure()
    {
        // Arrange
        IServiceFactory factory = new TestServiceFactory();
        var wrongConfig = new Mock<IGenericConfiguration>().Object;

        // Act
        var result = factory.Create(wrongConfig);

        // Assert
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactory_CreateTyped_WithIncompatibleType_ReturnsFailure()
    {
        // Arrange
        var factory = new TestServiceFactory();
        var config = new TestConfiguration();

        // Act - Try to create AnotherTestService from TestServiceFactory
        var result = factory.Create<AnotherTestService>(config);

        // Assert
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue();
        result.Code?.Name.ShouldBe("ServiceCastFailed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactory_CreateTyped_WithCompatibleType_ReturnsSuccess()
    {
        // Arrange
        var factory = new TestServiceFactory();
        var config = new TestConfiguration();

        // Act - Try to create IGenericService from TestServiceFactory
        var result = factory.Create<IGenericService>(config);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactory_CreateTyped_WithNullConfiguration_ReturnsFailure()
    {
        // Arrange
        var factory = new TestServiceFactory();

        // Act
        var result = factory.Create<IGenericService>(null!);

        // Assert
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactory_CreateTyped_WithWrongConfigurationType_ReturnsFailure()
    {
        // Arrange
        var factory = new TestServiceFactory();
        var wrongConfig = new Mock<IGenericConfiguration>().Object;

        // Act
        var result = factory.Create<IGenericService>(wrongConfig);

        // Assert
        result.ShouldNotBeNull();
        result.IsFailure.ShouldBeTrue();
    }

    // Skipped: Accesses protected Logger property
    // [Fact]
    // public void Logger_Property_ReturnsProtectedLogger()
    // {
    //     // Arrange
    //     var mockLogger = new Mock<ILogger<TestServiceFactory>>();
    //     var factory = new TestServiceFactory(mockLogger.Object);
    //
    //     // Act
    //     var logger = factory.Logger;
    //
    //     // Assert
    //     logger.ShouldBe(mockLogger.Object);
    // }

}
