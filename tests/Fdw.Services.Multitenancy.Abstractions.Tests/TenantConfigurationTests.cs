using Fdw.Configuration;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

public class TenantConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsDefaultValues()
    {
        // Act
        var result = new TenantConfiguration();

        // Assert
        result.Id.ShouldBe(Guid.Empty);
        result.Name.ShouldBe(string.Empty);
        result.Slug.ShouldBe(string.Empty);
        result.IsActive.ShouldBeTrue();
        result.ConnectionName.ShouldBeNull();
        result.Theme.ShouldNotBeNull();
        result.Options.ShouldNotBeNull();
        result.AvailableRoles.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ServiceTypeReturnsTenant()
    {
        // Arrange
        var config = new TenantConfiguration();

        // Act
        var result = config.ServiceType;

        // Assert
        result.ShouldBe("Tenant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ServiceOptionTypeReturnsNull()
    {
        // Arrange
        var config = new TenantConfiguration();

        // Act
        var result = config.ServiceOptionType;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IdCanBeSet()
    {
        // Arrange
        var config = new TenantConfiguration();
        var id = Guid.NewGuid();

        // Act
        config.Id = id;

        // Assert
        config.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void NameCanBeSet()
    {
        // Arrange
        var config = new TenantConfiguration();

        // Act
        config.Name = "Test Tenant";

        // Assert
        config.Name.ShouldBe("Test Tenant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void SlugCanBeSet()
    {
        // Arrange
        var config = new TenantConfiguration();

        // Act
        config.Slug = "test-tenant";

        // Assert
        config.Slug.ShouldBe("test-tenant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsActiveCanBeSetToTrue()
    {
        // Arrange
        var config = new TenantConfiguration { IsActive = false };

        // Act
        config.IsActive = true;

        // Assert
        config.IsActive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsActiveCanBeSetToFalse()
    {
        // Arrange
        var config = new TenantConfiguration();

        // Act
        config.IsActive = false;

        // Assert
        config.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConnectionNameCanBeSet()
    {
        // Arrange
        var config = new TenantConfiguration();

        // Act
        config.ConnectionName = "TenantDb";

        // Assert
        config.ConnectionName.ShouldBe("TenantDb");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ThemeCanBeSet()
    {
        // Arrange
        var config = new TenantConfiguration();
        var theme = new TenantThemeConfiguration { PrimaryColor = "#ff0000" };

        // Act
        config.Theme = theme;

        // Assert
        config.Theme.ShouldBe(theme);
        config.Theme.PrimaryColor.ShouldBe("#ff0000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void OptionsCanBeSet()
    {
        // Arrange
        var config = new TenantConfiguration();
        var options = new TenantOptionsConfiguration { MaxUsers = 100 };

        // Act
        config.Options = options;

        // Assert
        config.Options.ShouldBe(options);
        config.Options.MaxUsers.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AvailableRolesCanBeSet()
    {
        // Arrange
        var config = new TenantConfiguration();
        var roles = new List<string> { "Role1", "Role2" };

        // Act
        config.AvailableRoles = roles;

        // Assert
        config.AvailableRoles.ShouldBe(roles);
        config.AvailableRoles.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AvailableRolesDefaultsToEmpty()
    {
        // Act
        var config = new TenantConfiguration();

        // Assert
        // Why: per the no-default-values-on-configuration-properties rule, AvailableRoles
        // is initialized to an empty (non-null) collection, not seeded with Admin/User.
        config.AvailableRoles.ShouldNotBeNull();
        config.AvailableRoles.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void SectionNameUsesSlug()
    {
        // Arrange
        var config = new TenantConfiguration { Slug = "acme" };

        // Act
        var result = config.SectionName;

        // Assert
        result.ShouldBe("Tenants:acme");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void SectionNameUpdatesWithSlugChange()
    {
        // Arrange
        var config = new TenantConfiguration { Slug = "acme" };

        // Act
        config.Slug = "contoso";

        // Assert
        config.SectionName.ShouldBe("Tenants:contoso");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ImplementsIGenericConfiguration()
    {
        // Act
        var result = new TenantConfiguration();

        // Assert
        result.ShouldBeAssignableTo<IGenericConfiguration>();
    }
}
