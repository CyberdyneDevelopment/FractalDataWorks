using System;
using FluentValidation.TestHelper;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Validation;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests.Validation;

public sealed class RoleConfigurationValidatorTests
{
    private readonly RoleConfigurationValidator _validator = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithValidConfiguration()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithEmptyName()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = string.Empty
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithInvalidNamePattern()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "1Role"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithNameExceedingMaxLength()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = new string('A', 101)
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithNullDisplayName()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin",
            DisplayName = null
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithValidDisplayName()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin",
            DisplayName = "System Administrator"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithLongDisplayName()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin",
            DisplayName = new string('A', 201)
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithNullDescription()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin",
            Description = null
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithValidDescription()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin",
            Description = "Full system access"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithControlCharactersInDescription()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin",
            Description = "Invalid\0Description"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithDescriptionExceedingMaxLength()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin",
            Description = new string('A', 1001)
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithNegativeSortOrder()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin",
            SortOrder = -1
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortOrder);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithZeroSortOrder()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin",
            SortOrder = 0
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWhenTenantScopedWithValidTenantId()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "TenantAdmin",
            IsTenantScoped = true,
            TenantId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWhenTenantScopedWithEmptyTenantId()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "TenantAdmin",
            IsTenantScoped = true,
            TenantId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert - validator checks x.TenantId!.Value
        result.ShouldHaveValidationErrorFor("TenantId.Value");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWhenNotTenantScopedWithNullTenantId()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "GlobalAdmin",
            IsTenantScoped = false,
            TenantId = null
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWhenNotTenantScopedWithEmptyTenantId()
    {
        // Arrange - validator only checks when IsTenantScoped AND TenantId.HasValue
        var config = new RoleConfiguration
        {
            Name = "GlobalAdmin",
            IsTenantScoped = false,
            TenantId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateOptionsReturnsSuccessForValidConfig()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = "Admin"
        };

        // Act
        var result = _validator.Validate(Options.DefaultName, config);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateOptionsReturnsFailureForInvalidConfig()
    {
        // Arrange
        var config = new RoleConfiguration
        {
            Name = string.Empty,
            SortOrder = -1
        };

        // Act
        var result = _validator.Validate(Options.DefaultName, config);

        // Assert
        result.Failed.ShouldBeTrue();
    }
}
