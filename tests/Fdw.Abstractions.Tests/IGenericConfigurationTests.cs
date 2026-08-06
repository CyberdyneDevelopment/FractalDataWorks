using Fdw.Configuration;
using Moq;
using System;

namespace Fdw.Abstractions.Tests;

/// <summary>
/// Tests for IGenericConfiguration interface contracts.
/// </summary>
public class IGenericConfigurationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericConfigurationInterfaceExists()
    {
        // Assert
        var type = typeof(IGenericConfiguration);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericConfigurationHasIdProperty()
    {
        // Assert
        var type = typeof(IGenericConfiguration);
        var property = type.GetProperty("Id");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(Guid));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericConfigurationHasNameProperty()
    {
        // Assert
        var type = typeof(IGenericConfiguration);
        var property = type.GetProperty("Name");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericConfigurationHasSectionNameProperty()
    {
        // Assert
        var type = typeof(IGenericConfiguration);
        var property = type.GetProperty("SectionName");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericConfigurationHasServiceTypeProperty()
    {
        // Assert
        var type = typeof(IGenericConfiguration);
        var property = type.GetProperty("ServiceType");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericConfigurationHasServiceOptionTypeProperty()
    {
        // Assert
        var type = typeof(IGenericConfiguration);
        var property = type.GetProperty("ServiceOptionType");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericConfigurationGenericInterfaceExists()
    {
        // Assert
        var type = typeof(IGenericConfiguration<>);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericConfigurationGenericInheritsFromBase()
    {
        // Assert
        var type = typeof(IGenericConfiguration<>);
        var baseInterface = type.GetInterface("IGenericConfiguration");
        baseInterface.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationCanSetId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.Id).Returns(expectedId);

        // Act
        var id = mockConfig.Object.Id;

        // Assert
        id.ShouldBe(expectedId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationCanSetName()
    {
        // Arrange
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.Name).Returns("TestConfig");

        // Act
        var name = mockConfig.Object.Name;

        // Assert
        name.ShouldBe("TestConfig");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationCanSetSectionName()
    {
        // Arrange
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.SectionName).Returns("Connections:MsSql");

        // Act
        var sectionName = mockConfig.Object.SectionName;

        // Assert
        sectionName.ShouldBe("Connections:MsSql");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationCanSetServiceType()
    {
        // Arrange
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.ServiceType).Returns("Connection");

        // Act
        var serviceType = mockConfig.Object.ServiceType;

        // Assert
        serviceType.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationCanSetServiceOptionType()
    {
        // Arrange
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.ServiceOptionType).Returns("MsSql");

        // Act
        var serviceOptionType = mockConfig.Object.ServiceOptionType;

        // Assert
        serviceOptionType.ShouldBe("MsSql");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationServiceOptionTypeCanBeNull()
    {
        // Arrange
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.ServiceOptionType).Returns((string?)null);

        // Act
        var serviceOptionType = mockConfig.Object.ServiceOptionType;

        // Assert
        serviceOptionType.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationSupportsConnectionServiceType()
    {
        // Arrange
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.ServiceType).Returns("Connection");
        mockConfig.Setup(c => c.ServiceOptionType).Returns("MsSql");

        // Act & Assert
        mockConfig.Object.ServiceType.ShouldBe("Connection");
        mockConfig.Object.ServiceOptionType.ShouldBe("MsSql");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationSupportsAuthenticationServiceType()
    {
        // Arrange
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.ServiceType).Returns("Authentication");
        mockConfig.Setup(c => c.ServiceOptionType).Returns("Jwt");

        // Act & Assert
        mockConfig.Object.ServiceType.ShouldBe("Authentication");
        mockConfig.Object.ServiceOptionType.ShouldBe("Jwt");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationSupportsNotificationServiceType()
    {
        // Arrange
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.ServiceType).Returns("Notification");
        mockConfig.Setup(c => c.ServiceOptionType).Returns("Email");

        // Act & Assert
        mockConfig.Object.ServiceType.ShouldBe("Notification");
        mockConfig.Object.ServiceOptionType.ShouldBe("Email");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockConfigurationSupportsSecretManagerServiceType()
    {
        // Arrange
        var mockConfig = new Mock<IGenericConfiguration>();
        mockConfig.Setup(c => c.ServiceType).Returns("SecretManager");
        mockConfig.Setup(c => c.ServiceOptionType).Returns("AzureKeyVault");

        // Act & Assert
        mockConfig.Object.ServiceType.ShouldBe("SecretManager");
        mockConfig.Object.ServiceOptionType.ShouldBe("AzureKeyVault");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GenericConfigurationConstrainsTypeParameter()
    {
        // Arrange
        var type = typeof(IGenericConfiguration<>);
        var typeParam = type.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();

        // Assert
        constraints.ShouldNotBeEmpty();
        constraints.Length.ShouldBe(1);
    }
}
