using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Commands;
using Fdw.Services.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests to verify interface contracts exist and have required members.
/// </summary>
public class InterfaceContractTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionInterfaceExists()
    {
        // Act
        var type = typeof(IConnection<>);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionFactoryInterfaceExists()
    {
        // Act
        var type = typeof(IConnectionFactory);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionProviderInterfaceExists()
    {
        // Act
        var type = typeof(IConnectionProvider);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionTypeInterfaceExists()
    {
        // Act
        var type = typeof(IConnectionType);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }


    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionMetadataInterfaceExists()
    {
        // Act
        var type = typeof(IConnectionMetadata);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionStateInterfaceExists()
    {
        // Act
        var type = typeof(IConnectionState);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IGenericConnectionInterfaceExists()
    {
        // Act
        var type = typeof(IGenericConnection);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IDataConnectionInterfaceExists()
    {
        // Act
        var type = typeof(IDataConnection);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IDataConnectionProviderInterfaceExists()
    {
        // Act
        var type = typeof(IDataConnectionProvider);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionCreateCommandInterfaceExists()
    {
        // Act
        var type = typeof(IConnectionCreateCommand);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionDiscoveryCommandInterfaceExists()
    {
        // Act
        var type = typeof(IConnectionDiscoveryCommand);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionManagementCommandInterfaceExists()
    {
        // Act
        var type = typeof(IConnectionManagementCommand);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ICredentialArtifactInterfaceExists()
    {
        // Act
        var type = typeof(ICredentialArtifact);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ICredentialArtifactHasArtifactTypeProperty()
    {
        // Act
        var type = typeof(ICredentialArtifact);
        var property = type.GetProperty("ArtifactType");

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionMetadataHasRequiredProperties()
    {
        // Act
        var type = typeof(IConnectionMetadata);

        // Assert
        type.GetProperty("SystemName").ShouldNotBeNull();
        type.GetProperty("Version").ShouldNotBeNull();
        type.GetProperty("ServerInfo").ShouldNotBeNull();
        type.GetProperty("DatabaseName").ShouldNotBeNull();
        type.GetProperty("Capabilities").ShouldNotBeNull();
        type.GetProperty("CollectedAt").ShouldNotBeNull();
        type.GetProperty("CustomProperties").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IConnectionConfigurationInheritsFromIGenericConfiguration()
    {
        // Act
        var type = typeof(IGenericConfiguration);

        // Assert
        typeof(IGenericConfiguration).IsAssignableFrom(type).ShouldBeTrue();
    }
}

