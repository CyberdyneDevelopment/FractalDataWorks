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
        var config = new ServerSettingConfiguration
        {
            SettingName = "MaxRows",
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
        var config = new ServerSettingConfiguration
        {
            SettingName = string.Empty,
            SettingValue = "1000",
            DataType = "Int32"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SettingName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void EmptySettingValueFailsValidation()
    {
        // Arrange
        var config = new ServerSettingConfiguration
        {
            SettingName = "MaxRows",
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
        var config = new ServerSettingConfiguration
        {
            SettingName = "MaxRows",
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
        var config = new ServerSettingConfiguration
        {
            SettingName = "MaxRows",
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
        var config = new ServerSettingConfiguration
        {
            SettingName = "MaxRows",
            SettingValue = "1000",
            DataType = "Int32",
            Description = null
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
}
