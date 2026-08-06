using Fdw.Services.Connections.Http.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests;

public class HttpConnectionMetadataTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsSystemName()
    {
        // Arrange
        const string systemName = "TestSystem";

        // Act
        var metadata = new HttpConnectionMetadata(systemName);

        // Assert
        metadata.SystemName.ShouldBe(systemName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsCollectedAtToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var metadata = new HttpConnectionMetadata("Test");
        var after = DateTimeOffset.UtcNow;

        // Assert
        metadata.CollectedAt.ShouldBeGreaterThanOrEqualTo(before);
        metadata.CollectedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorInitializesEmptyCapabilities()
    {
        // Arrange & Act
        var metadata = new HttpConnectionMetadata("Test");

        // Assert
        metadata.Capabilities.ShouldNotBeNull();
        metadata.Capabilities.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorInitializesEmptyCustomProperties()
    {
        // Arrange & Act
        var metadata = new HttpConnectionMetadata("Test");

        // Assert
        metadata.CustomProperties.ShouldNotBeNull();
        metadata.CustomProperties.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void VersionReturnsNull()
    {
        // Arrange
        var metadata = new HttpConnectionMetadata("Test");

        // Act
        var version = metadata.Version;

        // Assert
        version.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ServerInfoReturnsNull()
    {
        // Arrange
        var metadata = new HttpConnectionMetadata("Test");

        // Act
        var serverInfo = metadata.ServerInfo;

        // Assert
        serverInfo.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DatabaseNameReturnsNull()
    {
        // Arrange
        var metadata = new HttpConnectionMetadata("Test");

        // Act
        var databaseName = metadata.DatabaseName;

        // Assert
        databaseName.ShouldBeNull();
    }

}
