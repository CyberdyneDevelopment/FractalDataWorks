using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

public class ConfiguredTenantTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorAcceptsTenantConfiguration()
    {
        // Arrange
        var config = new TenantConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Slug = "test-tenant"
        };

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsIdFromConfiguration()
    {
        // Arrange
        var id = Guid.NewGuid();
        var config = new TenantConfiguration { Id = id };

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsNameFromConfiguration()
    {
        // Arrange
        var config = new TenantConfiguration { Name = "Test Tenant" };

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.Name.ShouldBe("Test Tenant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsSlugFromConfiguration()
    {
        // Arrange
        var config = new TenantConfiguration { Slug = "test-tenant" };

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.Slug.ShouldBe("test-tenant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsIsActiveFromConfiguration()
    {
        // Arrange
        var config = new TenantConfiguration { IsActive = false };

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsConnectionNameFromConfiguration()
    {
        // Arrange
        var config = new TenantConfiguration { ConnectionName = "TenantDb" };

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.ConnectionName.ShouldBe("TenantDb");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsThemeFromConfiguration()
    {
        // Arrange
        var config = new TenantConfiguration
        {
            Theme = new TenantThemeConfiguration { PrimaryColor = "#ff0000" }
        };

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.Theme.PrimaryColor.ShouldBe("#ff0000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsOptionsFromConfiguration()
    {
        // Arrange
        var config = new TenantConfiguration
        {
            Options = new TenantOptionsConfiguration { MaxUsers = 100 }
        };

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.Options.MaxUsers.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsAvailableRolesFromConfiguration()
    {
        // Arrange
        var config = new TenantConfiguration
        {
            AvailableRoles = new List<string> { "Role1", "Role2" }
        };

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.AvailableRoles.Count().ShouldBe(2);
        result.AvailableRoles.ShouldContain("Role1");
        result.AvailableRoles.ShouldContain("Role2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void InheritsTenantTypeBase()
    {
        // Arrange
        var config = new TenantConfiguration();

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.ShouldBeAssignableTo<TenantTypeBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ImplementsITenant()
    {
        // Arrange
        var config = new TenantConfiguration();

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.ShouldBeAssignableTo<ITenant>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConfigurationSectionUsesSlug()
    {
        // Arrange
        var config = new TenantConfiguration { Slug = "acme" };

        // Act
        var result = new ConfiguredTenant(config);

        // Assert
        result.ConfigurationSection.ShouldBe("Tenants:acme");
    }
}
