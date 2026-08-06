using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class OAuth2SecurityConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new OAuth2SecurityConfiguration();

        // Assert
        config.Enabled.ShouldBeFalse();
        config.Authority.ShouldBe(string.Empty);
        config.ClientId.ShouldBe(string.Empty);
        config.ClientSecret.ShouldBe(string.Empty);
        config.RequiredScopes.ShouldNotBeNull();
        config.RequiredScopes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange & Act
        var config = new OAuth2SecurityConfiguration
        {
            Enabled = true,
            Authority = "https://auth.example.com",
            ClientId = "my-client-id",
            ClientSecret = "my-client-secret",
            RequiredScopes = ["read", "write", "admin"]
        };

        // Assert
        config.Enabled.ShouldBeTrue();
        config.Authority.ShouldBe("https://auth.example.com");
        config.ClientId.ShouldBe("my-client-id");
        config.ClientSecret.ShouldBe("my-client-secret");
        config.RequiredScopes.ShouldBe(new[] { "read", "write", "admin" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Enabled_CanBeSet()
    {
        // Arrange & Act
        var config = new OAuth2SecurityConfiguration { Enabled = true };

        // Assert
        config.Enabled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Authority_CanBeSet()
    {
        // Arrange & Act
        var config = new OAuth2SecurityConfiguration { Authority = "https://identity.server.com" };

        // Assert
        config.Authority.ShouldBe("https://identity.server.com");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ClientId_CanBeSet()
    {
        // Arrange & Act
        var config = new OAuth2SecurityConfiguration { ClientId = "client-123" };

        // Assert
        config.ClientId.ShouldBe("client-123");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ClientSecret_CanBeSet()
    {
        // Arrange & Act
        var config = new OAuth2SecurityConfiguration { ClientSecret = "secret-abc" };

        // Assert
        config.ClientSecret.ShouldBe("secret-abc");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RequiredScopes_CanBeSet()
    {
        // Arrange
        var scopes = new[] { "openid", "profile", "email" };

        // Act
        var config = new OAuth2SecurityConfiguration { RequiredScopes = scopes };

        // Assert
        config.RequiredScopes.ShouldBe(scopes);
    }
}
