using System;
using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class ApiKeyMetadataTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var metadata = new ApiKeyMetadata();

        // Assert
        metadata.Name.ShouldBe(string.Empty);
        metadata.Roles.ShouldNotBeNull();
        metadata.Roles.ShouldBeEmpty();
        metadata.Scopes.ShouldNotBeNull();
        metadata.Scopes.ShouldBeEmpty();
        metadata.ExpirationDate.ShouldBeNull();
        metadata.IsActive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange
        var expirationDate = DateTime.UtcNow.AddDays(30);

        // Act
        var metadata = new ApiKeyMetadata
        {
            Name = "Test API Key",
            Roles = ["Admin", "User"],
            Scopes = ["read", "write"],
            ExpirationDate = expirationDate,
            IsActive = false
        };

        // Assert
        metadata.Name.ShouldBe("Test API Key");
        metadata.Roles.ShouldBe(new[] { "Admin", "User" });
        metadata.Scopes.ShouldBe(new[] { "read", "write" });
        metadata.ExpirationDate.ShouldBe(expirationDate);
        metadata.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Name_CanBeSet()
    {
        // Arrange & Act
        var metadata = new ApiKeyMetadata { Name = "Production Key" };

        // Assert
        metadata.Name.ShouldBe("Production Key");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Roles_CanBeSet()
    {
        // Arrange & Act
        var roles = new[] { "Manager", "Developer" };
        var metadata = new ApiKeyMetadata { Roles = roles };

        // Assert
        metadata.Roles.ShouldBe(roles);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Scopes_CanBeSet()
    {
        // Arrange & Act
        var scopes = new[] { "api:read", "api:write", "api:delete" };
        var metadata = new ApiKeyMetadata { Scopes = scopes };

        // Assert
        metadata.Scopes.ShouldBe(scopes);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExpirationDate_CanBeSet()
    {
        // Arrange
        var expirationDate = new DateTime(2025, 12, 31);

        // Act
        var metadata = new ApiKeyMetadata { ExpirationDate = expirationDate };

        // Assert
        metadata.ExpirationDate.ShouldBe(expirationDate);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsActive_CanBeSet()
    {
        // Arrange & Act
        var metadata = new ApiKeyMetadata { IsActive = false };

        // Assert
        metadata.IsActive.ShouldBeFalse();
    }
}
