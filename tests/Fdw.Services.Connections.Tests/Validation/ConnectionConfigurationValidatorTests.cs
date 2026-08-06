using System;
using FluentValidation.TestHelper;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Validation;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests.Validation;

public sealed class ConnectionConfigurationValidatorTests
{
    private readonly ConnectionConfigurationValidator _validator = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidatePassesWithValidConfiguration()
    {
        // Arrange
        var config = new ConnectionConfiguration
        {
            Name = "TestConn",
            ServiceType = "Connection"
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
        var config = new ConnectionConfiguration
        {
            Name = string.Empty,
            ServiceType = "Connection"
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
        var config = new ConnectionConfiguration
        {
            Name = "1Connection",
            ServiceType = "Connection"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithNameExceedingMaxLength()
    {
        // Arrange
        var config = new ConnectionConfiguration
        {
            Name = new string('A', 201),
            ServiceType = "Connection"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateFailsWithEmptyServiceType()
    {
        // Arrange
        var config = new ConnectionConfiguration
        {
            Name = "TestConn",
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
        var config = new ConnectionConfiguration
        {
            Name = "TestConn",
            ServiceType = "Connection",
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
        var config = new ConnectionConfiguration
        {
            Name = "TestConn",
            ServiceType = "Connection",
            Description = "A valid test connection"
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
        var config = new ConnectionConfiguration
        {
            Name = "TestConn",
            ServiceType = "Connection",
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
        var config = new ConnectionConfiguration
        {
            Name = "TestConn",
            ServiceType = "Connection",
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
        var config = new ConnectionConfiguration
        {
            Name = "TestConn",
            ServiceType = "Connection"
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
        var config = new ConnectionConfiguration
        {
            Name = string.Empty,
            ServiceType = string.Empty
        };

        // Act
        var result = _validator.Validate(Options.DefaultName, config);

        // Assert
        result.Failed.ShouldBeTrue();
    }
}
