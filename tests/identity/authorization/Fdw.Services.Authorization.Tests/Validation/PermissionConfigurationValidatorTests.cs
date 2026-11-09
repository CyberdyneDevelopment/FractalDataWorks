using FluentValidation.TestHelper;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Validation;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests.Validation;

public sealed class PermissionConfigurationValidatorTests
{
    private readonly PermissionConfigurationValidator _validator = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithValidConfiguration()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-read",
            Domain = "Permission",
            PermissionDomain = "connections",
            Resource = "connections",
            Action = "read"
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
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = string.Empty,
            Resource = "connections",
            Action = "read"
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
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "1permission",
            Resource = "connections",
            Action = "read"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithEmptyResource()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-read",
            Resource = string.Empty,
            Action = "read"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Resource);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithLongResource()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "test-read",
            Resource = new string('a', 101),
            Action = "read"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Resource);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithUppercaseResource()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-read",
            Resource = "Connections",
            Action = "read"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Resource);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithResourceStartingWithNumber()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "test-read",
            Resource = "1connections",
            Action = "read"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Resource);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithResourceContainingDashesAndUnderscores()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "test-read",
            Resource = "data_store-items",
            Action = "read"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Resource);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithEmptyAction()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-test",
            Resource = "connections",
            Action = string.Empty
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Action);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithInvalidAction()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-browse",
            Resource = "connections",
            Action = "1browse"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Action);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("read")]
    [InlineData("write")]
    [InlineData("execute")]
    [InlineData("delete")]
    [InlineData("admin")]
    [InlineData("view")]
    [InlineData("query")]
    [InlineData("members")]
    [InlineData("delegate")]
    [InlineData("api-key")]
    public void ValidatePassesWithValidActions(string action)
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = $"connections-{action}",
            Resource = "connections",
            Action = action
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Action);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("READ")]
    [InlineData("Write")]
    [InlineData("EXECUTE")]
    public void ValidatePassesWithValidActionsInDifferentCase(string action)
    {
        // Arrange - validator uses OrdinalIgnoreCase
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-test",
            Resource = "connections",
            Action = action
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Action);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithThePermissionAWorkspaceIsGiven()
    {
        // FDW-4241: creating a workspace saves workspaces/{guid}:view; every Save was refused before the grammar matched what is stored.
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "workspaces/66666666-0000-0000-0000-000000000001:view",
            Domain = "Permission",
            Implementation = "Permission",
            PermissionDomain = "workspaces",
            Resource = "workspaces/66666666-0000-0000-0000-000000000001",
            Action = "view",
            DisplayName = "View workspace Acme",
            Category = "Workspaces"
        };

        var result = _validator.TestValidate(config);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("settings/role")]
    [InlineData("notifications/lists")]
    [InlineData("workspaces/0abc1234-0000-0000-0000-000000000001")]
    [InlineData("*")]
    public void ValidatePassesWithSlashSeparatedResources(string resource)
    {
        var config = new PermissionImplementationConfiguration
        {
            Scope = "global",
            Name = $"{resource}:read",
            Domain = "Permission",
            PermissionDomain = "settings",
            Resource = resource,
            Action = "read"
        };

        var result = _validator.TestValidate(config);

        result.ShouldNotHaveValidationErrorFor(x => x.Resource);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("/settings")]
    [InlineData("settings/")]
    [InlineData("settings//role")]
    [InlineData("settings/Role")]
    [InlineData("settings/-role")]
    [InlineData("settings role")]
    public void ValidateFailsWithMalformedResources(string resource)
    {
        var config = new PermissionImplementationConfiguration
        {
            Scope = "global",
            Name = "settings:read",
            PermissionDomain = "settings",
            Resource = resource,
            Action = "read"
        };

        var result = _validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.Resource);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithNoPermissionDomain()
    {
        var config = new PermissionImplementationConfiguration
        {
            Scope = "global",
            Name = "settings:read",
            PermissionDomain = string.Empty,
            Resource = "settings",
            Action = "read"
        };

        var result = _validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.PermissionDomain);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithNullCategory()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-read",
            Domain = "Permission",
            PermissionDomain = "connections",
            Resource = "connections",
            Action = "read",
            Category = null
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithValidCategory()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-read",
            Domain = "Permission",
            PermissionDomain = "connections",
            Resource = "connections",
            Action = "read",
            Category = "Data Access"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWithLongCategory()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-read",
            Resource = "connections",
            Action = "read",
            Category = new string('A', 101)
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidatePassesWithNullDescription()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-read",
            Domain = "Permission",
            PermissionDomain = "connections",
            Resource = "connections",
            Action = "read",
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
    public void ValidateFailsWithControlCharactersInDescription()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-read",
            Resource = "connections",
            Action = "read",
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
    public void ValidateOptionsReturnsSuccessForValidConfig()
    {
        // Arrange
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = "connections-read",
            Domain = "Permission",
            PermissionDomain = "connections",
            Resource = "connections",
            Action = "read"
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
        var config = new PermissionImplementationConfiguration
        {
            Scope = "tenant",
            Name = string.Empty,
            Resource = string.Empty,
            Action = string.Empty
        };

        // Act
        var result = _validator.Validate(Options.DefaultName, config);

        // Assert
        result.Failed.ShouldBeTrue();
    }
}
