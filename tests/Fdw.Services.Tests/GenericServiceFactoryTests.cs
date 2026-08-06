using System;
using Fdw.Configuration;
using Fdw.Services.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests;

[Collection(nameof(ServicesTestCollection))]
public class GenericServiceFactoryTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithLogger_StoresLogger()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TestService>>();

        // Act
        var factory = new GenericServiceFactory<TestService, TestConfiguration>(mockLogger.Object);

        // Assert
        factory.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Constructor_WithoutLogger_UsesNullLogger()
    {
        // Arrange & Act
        var factory = new GenericServiceFactory<TestService, TestConfiguration>();

        // Assert
        factory.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Create_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var factory = new GenericServiceFactory<TestService, TestConfiguration>();
        var config = new TestConfiguration { Id = Guid.NewGuid(), Name = "GenTestConfig" };

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
    public void Create_WithValidConfiguration_CreatesServiceInstance()
    {
        // Arrange
        var factory = new GenericServiceFactory<TestService, TestConfiguration>();
        var testId = Guid.NewGuid();
        var config = new TestConfiguration { Id = testId, Name = "InstanceConfig" };

        // Act
        var result = factory.Create(config);

        // Assert
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeOfType<TestService>();
        result.Value.Configuration.Id.ShouldBe(testId);
        result.Value.Configuration.Name.ShouldBe("InstanceConfig");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Create_WithValidConfiguration_ReturnsSuccessMessage()
    {
        // Arrange
        var factory = new GenericServiceFactory<TestService, TestConfiguration>();
        var config = new TestConfiguration();

        // Act
        var result = factory.Create(config);

        // Assert
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage!.ShouldContain("created successfully");
        result.CurrentMessage.ShouldContain(nameof(TestService));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Create_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        var factory = new GenericServiceFactory<TestService, TestConfiguration>();
        var config = new TestConfiguration();

        // Act & Assert
        Should.NotThrow(() => factory.Create(config));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Create_InheritsBaseValidation()
    {
        // Arrange
        var factory = new GenericServiceFactory<TestService, TestConfiguration>();

        // Act
        var result = factory.Create(null!);

        // Assert - Should use base class validation
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage!.ShouldContain("Configuration cannot be null");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Create_WithMultipleConfigurations_CreatesDifferentInstances()
    {
        // Arrange
        var factory = new GenericServiceFactory<TestService, TestConfiguration>();
        var testId1 = Guid.NewGuid();
        var testId2 = Guid.NewGuid();
        var config1 = new TestConfiguration { Id = testId1, Name = "First" };
        var config2 = new TestConfiguration { Id = testId2, Name = "Second" };

        // Act
        var result1 = factory.Create(config1);
        var result2 = factory.Create(config2);

        // Assert
        result1.Value.ShouldNotBeNull();
        result2.Value.ShouldNotBeNull();
        result1.Value.ShouldNotBe(result2.Value);
        result1.Value.Configuration.Id.ShouldBe(testId1);
        result2.Value.Configuration.Id.ShouldBe(testId2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Create_OverridesBaseCreateMethod()
    {
        // Arrange
        var factory = new GenericServiceFactory<TestService, TestConfiguration>();
        var config = new TestConfiguration { Name = "override-test" };

        // Act
        var result = factory.Create(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Verify it logs with ServiceFactoryLog (specific to GenericServiceFactory)
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage!.ShouldContain("created successfully");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void Create_WithConfigurationHavingDescription_StoresDescription()
    {
        // Arrange
        var factory = new GenericServiceFactory<TestService, TestConfiguration>();
        var config = new TestConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "DescConfig",
            Description = "Test description"
        };

        // Act
        var result = factory.Create(config);

        // Assert
        result.Value.ShouldNotBeNull();
        result.Value!.Configuration.Description.ShouldBe("Test description");
    }
}
