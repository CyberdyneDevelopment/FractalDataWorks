using Fdw.Collections;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

public class TenantTypeBaseTests
{
    [ExcludeFromCodeCoverage]
    private sealed class TestTenant : TenantTypeBase
    {
        public TestTenant(TenantConfiguration configuration) : base(configuration)
        {
        }

        public TestTenant(
            Guid id,
            string name,
            string slug,
            string? orgPrefix = null,
            string? connectionName = null,
            ITenantTheme? theme = null,
            ITenantOptions? options = null,
            IEnumerable<string>? availableRoles = null)
            : base(id, name, slug, orgPrefix, connectionName, theme, options, availableRoles)
        {
        }

        public void SetIsActive(bool value) => IsActive = value;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithConfigurationSetsId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var config = new TenantConfiguration { Id = id };

        // Act
        var result = new TestTenant(config);

        // Assert
        result.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithConfigurationSetsName()
    {
        // Arrange
        var config = new TenantConfiguration { Name = "Test Tenant" };

        // Act
        var result = new TestTenant(config);

        // Assert
        result.Name.ShouldBe("Test Tenant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithConfigurationSetsSlug()
    {
        // Arrange
        var config = new TenantConfiguration { Slug = "test-tenant" };

        // Act
        var result = new TestTenant(config);

        // Assert
        result.Slug.ShouldBe("test-tenant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithConfigurationSetsIsActive()
    {
        // Arrange
        var config = new TenantConfiguration { IsActive = false };

        // Act
        var result = new TestTenant(config);

        // Assert
        result.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithConfigurationSetsDisplayName()
    {
        // Arrange
        var config = new TenantConfiguration { Name = "Test Tenant" };

        // Act
        var result = new TestTenant(config);

        // Assert
        result.DisplayName.ShouldBe("Test Tenant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithConfigurationSetsConnectionName()
    {
        // Arrange
        var config = new TenantConfiguration { ConnectionName = "TenantDb" };

        // Act
        var result = new TestTenant(config);

        // Assert
        result.ConnectionName.ShouldBe("TenantDb");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithConfigurationSetsTheme()
    {
        // Arrange
        var config = new TenantConfiguration
        {
            Theme = new TenantThemeConfiguration { PrimaryColor = "#ff0000" }
        };

        // Act
        var result = new TestTenant(config);

        // Assert
        result.Theme.ShouldNotBeNull();
        result.Theme.PrimaryColor.ShouldBe("#ff0000");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithConfigurationSetsOptions()
    {
        // Arrange
        var config = new TenantConfiguration
        {
            Options = new TenantOptionsConfiguration { MaxUsers = 100 }
        };

        // Act
        var result = new TestTenant(config);

        // Assert
        result.Options.ShouldNotBeNull();
        result.Options.MaxUsers.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithConfigurationSetsAvailableRoles()
    {
        // Arrange
        var config = new TenantConfiguration
        {
            AvailableRoles = new List<string> { "Role1", "Role2" }
        };

        // Act
        var result = new TestTenant(config);

        // Assert
        result.AvailableRoles.Count().ShouldBe(2);
        result.AvailableRoles.ShouldContain("Role1");
        result.AvailableRoles.ShouldContain("Role2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithConfigurationSetsConfigurationSection()
    {
        // Arrange
        var config = new TenantConfiguration { Slug = "test-tenant" };

        // Act
        var result = new TestTenant(config);

        // Assert
        result.ConfigurationSection.ShouldBe("Tenants:test-tenant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = new TestTenant(id, "Test", "test");

        // Assert
        result.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsName()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test Name", "test");

        // Assert
        result.Name.ShouldBe("Test Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsSlug()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test-slug");

        // Assert
        result.Slug.ShouldBe("test-slug");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsIsActiveToTrue()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test");

        // Assert
        result.IsActive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsDisplayName()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test Name", "test");

        // Assert
        result.DisplayName.ShouldBe("Test Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsConnectionName()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test", connectionName: "TenantDb");

        // Assert
        result.ConnectionName.ShouldBe("TenantDb");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesUsesDefaultThemeWhenNull()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test");

        // Assert
        result.Theme.ShouldBe(TenantTheme.Default);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsCustomTheme()
    {
        // Arrange
        var theme = new TenantTheme { PrimaryColor = "#ff0000" };

        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test", theme: theme);

        // Assert
        result.Theme.ShouldBe(theme);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesUsesDefaultOptionsWhenNull()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test");

        // Assert
        result.Options.ShouldBe(TenantOptions.Default);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsCustomOptions()
    {
        // Arrange
        var options = new TenantOptions { MaxUsers = 100 };

        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test", options: options);

        // Assert
        result.Options.ShouldBe(options);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesUsesDefaultRolesWhenNull()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test");

        // Assert
        result.AvailableRoles.Count().ShouldBe(2);
        result.AvailableRoles.ShouldContain("Admin");
        result.AvailableRoles.ShouldContain("User");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsCustomRoles()
    {
        // Arrange
        var roles = new[] { "Role1", "Role2", "Role3" };

        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test", availableRoles: roles);

        // Assert
        result.AvailableRoles.Count().ShouldBe(3);
        result.AvailableRoles.ShouldContain("Role1");
        result.AvailableRoles.ShouldContain("Role2");
        result.AvailableRoles.ShouldContain("Role3");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsConfigurationSection()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test-slug");

        // Assert
        result.ConfigurationSection.ShouldBe("Tenants:test-slug");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsActiveCanBeModifiedByDerivedClass()
    {
        // Arrange
        var tenant = new TestTenant(Guid.NewGuid(), "Test", "test");

        // Act
        tenant.SetIsActive(false);

        // Assert
        tenant.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ImplementsITenant()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test");

        // Assert
        result.ShouldBeAssignableTo<ITenant>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ImplementsITypeOption()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test");

        // Assert
        result.ShouldBeAssignableTo<ITypeOption<Guid, ITenant>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithExplicitValuesSetsConnectionNameToNull()
    {
        // Act
        var result = new TestTenant(Guid.NewGuid(), "Test", "test");

        // Assert
        result.ConnectionName.ShouldBeNull();
    }
}
