using System;
using FluentValidation.TestHelper;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Validation;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests.Validation;

public sealed class DataStoreConfigurationValidatorTests
{
    private readonly DataStoreConfigurationValidator _validator = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidatePassesWithValidConfiguration()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = "TestStore",
            ConnectionId = Guid.NewGuid(),
            ServiceType = "DataStore"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithEmptyName()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = string.Empty,
            ConnectionId = Guid.NewGuid(),
            ServiceType = "DataStore"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithInvalidNamePattern()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = "1DataStore",
            ConnectionId = Guid.NewGuid(),
            ServiceType = "DataStore"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithEmptyConnectionId()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = "TestStore",
            ConnectionId = Guid.Empty,
            ServiceType = "DataStore"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConnectionId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithEmptyServiceType()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = "TestStore",
            ConnectionId = Guid.NewGuid(),
            ServiceType = string.Empty
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ServiceType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidatePassesWithNullDescription()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = "TestStore",
            ConnectionId = Guid.NewGuid(),
            ServiceType = "DataStore",
            Description = null
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidatePassesWithValidDescription()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = "TestStore",
            ConnectionId = Guid.NewGuid(),
            ServiceType = "DataStore",
            Description = "A valid test data store"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithControlCharactersInDescription()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = "TestStore",
            ConnectionId = Guid.NewGuid(),
            ServiceType = "DataStore",
            Description = "Invalid\0Description"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithDescriptionExceedingMaxLength()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = "TestStore",
            ConnectionId = Guid.NewGuid(),
            ServiceType = "DataStore",
            Description = new string('A', 1001)
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateOptionsReturnsSuccessForValidConfig()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = "TestStore",
            ConnectionId = Guid.NewGuid(),
            ServiceType = "DataStore"
        };

        // Act
        var result = _validator.Validate(Options.DefaultName, config);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateOptionsReturnsFailureForInvalidConfig()
    {
        // Arrange
        var config = new DataStoreConfiguration
        {
            Name = string.Empty,
            ConnectionId = Guid.Empty,
            ServiceType = string.Empty
        };

        // Act
        var result = _validator.Validate(Options.DefaultName, config);

        // Assert
        result.Failed.ShouldBeTrue();
    }
}
