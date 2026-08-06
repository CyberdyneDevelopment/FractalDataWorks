using System;
using FluentValidation.TestHelper;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Validation;
using Xunit;

namespace Fdw.Services.Settings.Tests;

public sealed class TenantSettingConfigurationValidatorTests
{
    private readonly TenantSettingConfigurationValidator _validator = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidConfigurationPassesValidation()
    {
        // Arrange
        var config = new TenantSettingConfiguration
        {
            TenantId = Guid.NewGuid(),
            SettingName = "MaxRows",
            SettingValue = "500"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void EmptyTenantIdFailsValidation()
    {
        // Arrange
        var config = new TenantSettingConfiguration
        {
            TenantId = Guid.Empty,
            SettingName = "MaxRows",
            SettingValue = "500"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void EmptySettingNameFailsValidation()
    {
        // Arrange
        var config = new TenantSettingConfiguration
        {
            TenantId = Guid.NewGuid(),
            SettingName = string.Empty,
            SettingValue = "500"
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
        var config = new TenantSettingConfiguration
        {
            TenantId = Guid.NewGuid(),
            SettingName = "MaxRows",
            SettingValue = string.Empty
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SettingValue);
    }
}
