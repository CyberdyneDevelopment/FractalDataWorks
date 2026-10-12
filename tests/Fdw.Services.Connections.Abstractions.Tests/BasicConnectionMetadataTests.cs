using Fdw.Services.Connections.Abstractions;
using System.Reflection;

namespace Fdw.Services.Connections.Abstractions.Tests;

/// <summary>
/// Tests for BasicConnectionMetadata.
/// Note: BasicConnectionMetadata is internal, so we test it via reflection or through interfaces.
/// </summary>
public class BasicConnectionMetadataTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataTypeExists()
    {
        // Act
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");

        // Assert
        type.ShouldNotBeNull();
        type.IsClass.ShouldBeTrue();
        type.IsSealed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataImplementsIConnectionMetadata()
    {
        // Act
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");

        // Assert
        type.ShouldNotBeNull();
        typeof(IConnectionMetadata).IsAssignableFrom(type).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataHasConstructorWithSystemName()
    {
        // Act
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");
        var constructor = type?.GetConstructor(new[] { typeof(string) });

        // Assert
        constructor.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataCanBeInstantiated()
    {
        // Arrange
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");
        var constructor = type?.GetConstructor(new[] { typeof(string) });

        // Act
        var instance = constructor?.Invoke(new object[] { "TestSystem" }) as IConnectionMetadata;

        // Assert
        instance.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataStoresSystemName()
    {
        // Arrange
        var systemName = "TestSystem";
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");
        var constructor = type?.GetConstructor(new[] { typeof(string) });

        // Act
        var instance = constructor?.Invoke(new object[] { systemName }) as IConnectionMetadata;

        // Assert
        instance.ShouldNotBeNull();
        instance.SystemName.ShouldBe(systemName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataVersionIsNull()
    {
        // Arrange
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");
        var constructor = type?.GetConstructor(new[] { typeof(string) });

        // Act
        var instance = constructor?.Invoke(new object[] { "TestSystem" }) as IConnectionMetadata;

        // Assert
        instance.ShouldNotBeNull();
        instance.Version.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataServerInfoIsNull()
    {
        // Arrange
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");
        var constructor = type?.GetConstructor(new[] { typeof(string) });

        // Act
        var instance = constructor?.Invoke(new object[] { "TestSystem" }) as IConnectionMetadata;

        // Assert
        instance.ShouldNotBeNull();
        instance.ServerInfo.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataDatabaseNameIsNull()
    {
        // Arrange
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");
        var constructor = type?.GetConstructor(new[] { typeof(string) });

        // Act
        var instance = constructor?.Invoke(new object[] { "TestSystem" }) as IConnectionMetadata;

        // Assert
        instance.ShouldNotBeNull();
        instance.DatabaseName.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataCapabilitiesIsEmpty()
    {
        // Arrange
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");
        var constructor = type?.GetConstructor(new[] { typeof(string) });

        // Act
        var instance = constructor?.Invoke(new object[] { "TestSystem" }) as IConnectionMetadata;

        // Assert
        instance.ShouldNotBeNull();
        instance.Capabilities.ShouldNotBeNull();
        instance.Capabilities.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataCustomPropertiesIsEmpty()
    {
        // Arrange
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");
        var constructor = type?.GetConstructor(new[] { typeof(string) });

        // Act
        var instance = constructor?.Invoke(new object[] { "TestSystem" }) as IConnectionMetadata;

        // Assert
        instance.ShouldNotBeNull();
        instance.CustomProperties.ShouldNotBeNull();
        instance.CustomProperties.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void BasicConnectionMetadataCollectedAtIsSet()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var assembly = typeof(IConnectionMetadata).Assembly;
        var type = assembly.GetType("Fdw.Services.Connections.Abstractions.BasicConnectionMetadata");
        var constructor = type?.GetConstructor(new[] { typeof(string) });

        // Act
        var instance = constructor?.Invoke(new object[] { "TestSystem" }) as IConnectionMetadata;
        var after = DateTimeOffset.UtcNow;

        // Assert
        instance.ShouldNotBeNull();
        instance.CollectedAt.ShouldBeGreaterThanOrEqualTo(before);
        instance.CollectedAt.ShouldBeLessThanOrEqualTo(after);
    }
}
