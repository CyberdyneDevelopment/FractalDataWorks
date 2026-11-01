using System;
using FluentValidation.TestHelper;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Validation;
using Xunit;

namespace Fdw.Services.Settings.Tests;

public sealed class ServerSettingConfigurationValidatorTests
{
    private readonly ServerSettingConfigurationValidator _validator = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidConfigurationPassesValidation()
    {
        // Arrange
        var config = new ServerSettingImplementationConfiguration
        {
            Name = "MaxRows",
            SettingValue = "1000",
            DataType = "Int32"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void EmptySettingNameFailsValidation()
    {
        // Arrange
        var config = new ServerSettingImplementationConfiguration
        {
            Name = string.Empty,
            SettingValue = "1000",
            DataType = "Int32"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void EmptySettingValueFailsValidation()
    {
        // Arrange
        var config = new ServerSettingImplementationConfiguration
        {
            Name = "MaxRows",
            SettingValue = string.Empty,
            DataType = "Int32"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SettingValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void EmptyDataTypeFailsValidation()
    {
        // Arrange
        var config = new ServerSettingImplementationConfiguration
        {
            Name = "MaxRows",
            SettingValue = "1000",
            DataType = string.Empty
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DataType);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void DataTypeExceedingMaxLengthFailsValidation()
    {
        // Arrange
        var config = new ServerSettingImplementationConfiguration
        {
            Name = "MaxRows",
            SettingValue = "1000",
            DataType = new string('x', 65)
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DataType);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void NullDescriptionPassesValidation()
    {
        // Arrange
        var config = new ServerSettingImplementationConfiguration
        {
            Name = "MaxRows",
            SettingValue = "1000",
            DataType = "Int32",
            Description = null
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    // The two ApiKey paging settings are seeded 0, which means no limit: a stored 0 has to be a valid value (FDW-1209).
    [Theory]
    [InlineData("ApiKeyDefaultPaginationSize")]
    [InlineData("ApiKeyMaxPaginationSize")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ANoLimitValueOfZeroPassesValidationForTheApiKeyPagingSettings(string name)
    {
        // Arrange
        var config = new ServerSettingImplementationConfiguration
        {
            Name = name,
            SettingValue = "0",
            DataType = "Int32"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
