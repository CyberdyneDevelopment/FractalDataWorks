using System;
using Fdw.Services.SecretManagers;

namespace Fdw.Services.SecretManagers.Tests;

/// <summary>
/// Tests for SecretManagerConfigurationBase abstract class properties.
/// </summary>
public sealed class SecretManagerConfigurationBaseTests
{
    /// <summary>
    /// Concrete test implementation of the abstract SecretManagerConfigurationBase.
    /// </summary>
    private sealed class TestSecretManagerConfiguration : SecretManagerConfigurationBase
    {
        public override string SectionName => "TestSecretManagers";
        public override string SecretManagerType => "TestType";
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ServiceTypeReturnsSecretManager()
    {
        // Arrange
        var sut = new TestSecretManagerConfiguration();

        // Act
        var serviceType = sut.ServiceType;

        // Assert
        serviceType.ShouldBe("SecretManager");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ServiceOptionTypeReturnsSecretManagerType()
    {
        // Arrange
        var sut = new TestSecretManagerConfiguration();

        // Act
        var serviceOptionType = sut.ServiceOptionType;

        // Assert
        serviceOptionType.ShouldBe("TestType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SecretManagerTypeReturnsExpectedValue()
    {
        // Arrange
        var sut = new TestSecretManagerConfiguration();

        // Act & Assert
        sut.SecretManagerType.ShouldBe("TestType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SectionNameReturnsExpectedValue()
    {
        // Arrange
        var sut = new TestSecretManagerConfiguration();

        // Act & Assert
        sut.SectionName.ShouldBe("TestSecretManagers");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IdDefaultsToNewGuid()
    {
        // Arrange & Act
        var sut = new TestSecretManagerConfiguration();

        // Assert
        sut.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NameDefaultsToEmpty()
    {
        // Arrange & Act
        var sut = new TestSecretManagerConfiguration();

        // Assert
        sut.Name.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NameCanBeSet()
    {
        // Arrange
        var sut = new TestSecretManagerConfiguration { Name = "MyVault" };

        // Act & Assert
        sut.Name.ShouldBe("MyVault");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IdCanBeSet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sut = new TestSecretManagerConfiguration { Id = id };

        // Act & Assert
        sut.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DescriptionDefaultsToNull()
    {
        // Arrange & Act
        var sut = new TestSecretManagerConfiguration();

        // Assert
        sut.Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DescriptionCanBeSet()
    {
        // Arrange
        var sut = new TestSecretManagerConfiguration { Description = "Test vault" };

        // Act & Assert
        sut.Description.ShouldBe("Test vault");
    }
}
